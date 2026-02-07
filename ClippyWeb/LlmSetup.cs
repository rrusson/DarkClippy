using ClippyWeb.Util;

using Microsoft.Extensions.Caching.Memory;

using SemanticKernelHelper;

using Serilog;

using SharedInterfaces;

namespace ClippyWeb
{
	internal static class LlmSetup
	{

		/// <summary>
		/// Configures and registers the LLM chat service client with the application's dependency injection container.
		/// </summary>
		/// <param name="builder">The WebApplicationBuilder used to configure services and access application configuration settings.</param>
		/// <exception cref="InvalidOperationException">Thrown if the required configuration values for 'ServiceUrl' or 'Model' are missing or empty.</exception>
		/// <remarks>This method must be called during application startup to ensure that the IChatClient service is available for dependency injection.
		/// The method expects the application's configuration to provide valid values for ServiceUrl and Model.</remarks>
		internal static async Task SetupLlmService(WebApplicationBuilder builder)
		{
			builder.Services.AddSingleton<IPingService, PingService>();
			builder.Services.AddSingleton<ITcpClientFactory, TcpClientFactory>();
			builder.Services.AddSingleton<IConnectionValidator, ConnectionValidator>();

			using var scope = builder.Services.BuildServiceProvider().CreateScope();
			var validator = scope.ServiceProvider.GetRequiredService<IConnectionValidator>();
			await validator.ValidateConnectionAsync(builder.Configuration).ConfigureAwait(false);

			// Set up Model Capability Detector
			builder.Services.AddSingleton<IModelCapabilityDetector>(provider =>
			{
				string? serviceUrl = builder.Configuration["ServiceUrl"];
				if (string.IsNullOrEmpty(serviceUrl))
				{
					throw new InvalidOperationException("Please supply a config value for ServiceUrl.");
				}

				var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
				var logger = loggerFactory.CreateLogger("DarkClippy.ModelCapability");
				return new SemanticKernelHelper.OllamaCapabilityDetector(serviceUrl, logger);
			});

			// Initialize MCP servers early (before DI registration)
			var mcpRegistry = await InitializeMcpServersAsync(builder).ConfigureAwait(false);

			// Set up MCP Server Registry
			builder.Services.AddSingleton<IMcpServerRegistry>(mcpRegistry);

			builder.Services.AddSingleton<IChatClientFactory>(provider =>
			{
				return AddChatClient(builder, provider);
			});
		}

		private static async Task<SemanticKernelHelper.McpServerRegistry> InitializeMcpServersAsync(WebApplicationBuilder builder)
		{
			var registry = new SemanticKernelHelper.McpServerRegistry();
			
			// Build a temporary service provider to get logger
			using var tempProvider = builder.Services.BuildServiceProvider();
			var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DarkClippy.MCP");
			
			var mcpConfigs = builder.Configuration.GetSection("McpServers").Get<List<SemanticKernelHelper.McpServerConfiguration>>();

			if (mcpConfigs != null)
			{
				foreach (var config in mcpConfigs)
				{
					if (config.Enabled)
					{
						try
						{
							var mcpServer = new SemanticKernelHelper.StdioMcpServer(config, logger);
							await mcpServer.InitializeAsync().ConfigureAwait(false);
							registry.Register(mcpServer);
							Log.Information("DarkClippy: MCP server '{Name}' registered and initialized", config.Name);
						}
						catch (Exception ex)
						{
							Log.Warning(ex, "DarkClippy: Failed to initialize MCP server '{Name}', skipping", config.Name);
						}
					}
					else
					{
						Log.Information("DarkClippy: MCP server '{Name}' is disabled, skipping", config.Name);
					}
				}
			}

			return registry;
		}

		private static SemanticKernelHelper.ChatClientFactory AddChatClient(WebApplicationBuilder builder, IServiceProvider provider)
		{
			string? serviceUrl = builder.Configuration["ServiceUrl"];
			if (string.IsNullOrEmpty(serviceUrl))
			{
				throw new InvalidOperationException("Please supply a config value for ServiceUrl.");
			}

			string? model = builder.Configuration["Model"];
			if (string.IsNullOrEmpty(model))
			{
				throw new InvalidOperationException("Please supply a config value for Model.");
			}

			string? apiKey = builder.Configuration["ApiKey"];

			Log.Information("DarkClippy: Connecting to LLM service at: {ServiceUrl} with model: {Model}", serviceUrl, model);

			var cache = provider.GetRequiredService<IMemoryCache>();
			var mcpRegistry = provider.GetRequiredService<IMcpServerRegistry>();
			var capabilityDetector = provider.GetRequiredService<IModelCapabilityDetector>();
			var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
			var logger = loggerFactory.CreateLogger("DarkClippy.SemanticKernel");

			return new SemanticKernelHelper.ChatClientFactory(serviceUrl, model, apiKey ?? string.Empty, cache, mcpRegistry, capabilityDetector, logger);
		}
	}
}