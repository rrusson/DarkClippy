using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using ModelContextProtocol.Client;

using SemanticKernelHelper;

using SharedInterfaces;

using System.ComponentModel;

namespace SemanticKernelHelper
{
	/// <summary>
	/// MCP client implementation using the official ModelContextProtocol SDK.
	/// Connects to MCP servers via stdio or HTTP transports.
	/// </summary>
	public class McpGatewayClient : IMcpServer, IAsyncDisposable
	{
		private readonly McpServerConfiguration _configuration;
		private readonly ILogger? _logger;
		private dynamic? _mcpClient;
		private bool _initialized;
		private IList<McpClientTool>? _cachedTools;
		private readonly SemaphoreSlim _initLock = new(1, 1);

		/// <summary>
		/// Initializes a new instance of the McpGatewayClient class.
		/// </summary>
		/// <param name="configuration">The configuration for this MCP client.</param>
		/// <param name="logger">Optional logger for diagnostic information.</param>
		public McpGatewayClient(McpServerConfiguration configuration, ILogger? logger = null)
		{
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_logger = logger;
		}

		/// <summary>
		/// Gets the unique identifier for this MCP server.
		/// </summary>
		public string Name => _configuration.Name;

		/// <summary>
		/// Gets a description of the MCP server's capabilities.
		/// </summary>
		public string Description => _configuration.Description;

		/// <summary>
		/// Gets a value indicating whether this MCP server is enabled.
		/// </summary>
		public bool IsEnabled => _configuration.Enabled;

		/// <summary>
		/// Initializes the MCP client connection.
		/// </summary>
		/// <returns>A task that represents the asynchronous initialization operation.</returns>
		public async Task InitializeAsync()
		{
			await _initLock.WaitAsync().ConfigureAwait(false);
			try
			{
				if (_initialized)
				{
					return;
				}

				_logger?.LogInformation("Initializing MCP client: {Name}", Name);

				// Create the MCP client based on server type
				if (_configuration.ServerType.Equals("stdio", StringComparison.OrdinalIgnoreCase))
				{
					_mcpClient = await CreateStdioClientAsync().ConfigureAwait(false);
				}
				else if (_configuration.ServerType.Equals("http", StringComparison.OrdinalIgnoreCase))
				{
					_mcpClient = await CreateHttpClientAsync().ConfigureAwait(false);
				}
				else
				{
					throw new InvalidOperationException($"Unsupported server type: {_configuration.ServerType}");
				}

				_initialized = true;
				_logger?.LogInformation("MCP client '{Name}' initialized successfully", Name);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to initialize MCP client '{Name}'", Name);
				throw;
			}
			finally
			{
				_initLock.Release();
			}
		}

		/// <summary>
		/// Creates a stdio-based MCP client for local MCP server processes.
		/// </summary>
		/// <returns>A configured MCP client.</returns>
		private async Task<dynamic> CreateStdioClientAsync()
		{
			if (string.IsNullOrEmpty(_configuration.Command))
			{
				throw new InvalidOperationException($"Command is required for stdio MCP client '{Name}'");
			}

			// Resolve command for Windows
			string command = _configuration.Command;
			if (OperatingSystem.IsWindows() &&
				(command.Equals("npx", StringComparison.OrdinalIgnoreCase) ||
				 command.Equals("npm", StringComparison.OrdinalIgnoreCase)))
			{
				command += ".cmd";
			}

			var transportConfig = new StdioClientTransportConfig
			{
				Command = command,
				Arguments = _configuration.Arguments?.ToList() ?? [],
				EnvironmentVariables = _configuration.EnvironmentVariables ?? new Dictionary<string, string>(),
				Name = Name
			};

			if (!string.IsNullOrEmpty(_configuration.WorkingDirectory))
			{
				transportConfig.WorkingDirectory = _configuration.WorkingDirectory;
			}

			var transport = new StdioClientTransport(transportConfig);
			return await McpClientFactory.CreateAsync(transport).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates an HTTP-based MCP client for remote MCP servers or gateways.
		/// </summary>
		/// <returns>A configured MCP client.</returns>
		private async Task<dynamic> CreateHttpClientAsync()
		{
			if (string.IsNullOrEmpty(_configuration.Endpoint))
			{
				throw new InvalidOperationException($"Endpoint is required for HTTP MCP client '{Name}'");
			}

			if (!Uri.TryCreate(_configuration.Endpoint, UriKind.Absolute, out var endpointUri))
			{
				throw new InvalidOperationException($"Invalid endpoint URL for MCP client '{Name}': {_configuration.Endpoint}");
			}

			var transport = new HttpClientTransport(endpointUri);
			return await McpClientFactory.CreateAsync(transport).ConfigureAwait(false);
		}

		/// <summary>
		/// Gets the available tools from this MCP server.
		/// </summary>
		/// <returns>A collection of tool definitions available from this server.</returns>
		public async Task<IEnumerable<object>> GetToolsAsync()
		{
			if (!_initialized || _mcpClient == null)
			{
				throw new InvalidOperationException($"MCP client '{Name}' is not initialized. Call InitializeAsync first.");
			}

			if (_cachedTools != null)
			{
				return _cachedTools;
			}

			try
			{
				_cachedTools = await _mcpClient.ListToolsAsync().ConfigureAwait(false);
				_logger?.LogInformation("MCP client '{Name}' has {ToolCount} tools available", Name, _cachedTools.Count);
				return _cachedTools;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to list tools from MCP client '{Name}'", Name);
				return [];
			}
		}

		/// <summary>
		/// Calls a tool on the MCP server.
		/// </summary>
		/// <param name="toolName">The name of the tool to call.</param>
		/// <param name="arguments">The arguments to pass to the tool.</param>
		/// <returns>The result from the tool call.</returns>
		public async Task<string> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null)
		{
			if (!_initialized || _mcpClient == null)
			{
				throw new InvalidOperationException($"MCP client '{Name}' is not initialized. Call InitializeAsync first.");
			}

			try
			{
				_logger?.LogDebug("Calling tool '{ToolName}' on MCP client '{ClientName}'", toolName, Name);

				var result = await _mcpClient.CallToolAsync(toolName, arguments).ConfigureAwait(false);

				if (result?.IsError == true)
				{
					var errorText = result.Content?.FirstOrDefault()?.Text ?? "Unknown error";
					_logger?.LogError("MCP tool '{ToolName}' on client '{ClientName}' failed: {Error}", toolName, Name, errorText);
					return $"Tool error: {errorText}";
				}

				var resultText = string.Join("\n", result?.Content?.Select(c => c.Text ?? string.Empty) ?? []);
				_logger?.LogInformation("MCP tool '{ToolName}' on client '{ClientName}' completed successfully", toolName, Name);
				return resultText;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to call MCP tool '{ToolName}' on client '{ClientName}'", toolName, Name);
				return $"Exception calling tool: {ex.Message}";
			}
		}

		/// <summary>
		/// Creates a Semantic Kernel plugin from this MCP client.
		/// </summary>
		/// <returns>A kernel plugin that can be added to Semantic Kernel.</returns>
		public async Task<KernelPlugin> CreatePluginAsync()
		{
			// Get tools from the MCP server
			var tools = (await GetToolsAsync().ConfigureAwait(false)).OfType<McpClientTool>().ToList();

			if (tools.Count == 0)
			{
				_logger?.LogWarning("MCP client '{Name}' has no tools available", Name);
				// Return empty plugin if no tools
				string sanitizedName = SanitizePluginName(Name);
				return KernelPluginFactory.CreateFromObject(new McpPlugin(this), sanitizedName);
			}

			// Create kernel functions for each tool
			var functions = new List<KernelFunction>();
			foreach (var tool in tools)
			{
				try
				{
					var function = CreateKernelFunctionForTool(tool);
					functions.Add(function);
				}
				catch (Exception ex)
				{
					_logger?.LogWarning(ex, "Failed to create kernel function for tool '{ToolName}' on client '{ClientName}'", tool.Name, Name);
				}
			}

			string pluginName = SanitizePluginName(Name);
			return KernelPluginFactory.CreateFromFunctions(pluginName, Description, functions);
		}

		/// <summary>
		/// Creates a KernelFunction for an MCP tool.
		/// </summary>
		/// <param name="tool">The MCP tool definition.</param>
		/// <returns>A KernelFunction that invokes the MCP tool.</returns>
		private KernelFunction CreateKernelFunctionForTool(McpClientTool tool)
		{
			// Create parameters for the function from the tool's input schema
			var parameters = new List<KernelParameterMetadata>();
			if (tool.InputSchema?.Properties != null)
			{
				foreach (var prop in tool.InputSchema.Properties)
				{
					var isRequired = tool.InputSchema.Required?.Contains(prop.Key) ?? false;
					var parameter = new KernelParameterMetadata(prop.Key)
					{
						Description = prop.Value.Description ?? string.Empty,
						IsRequired = isRequired,
						ParameterType = typeof(string)
					};
					parameters.Add(parameter);
				}
			}

			// Create the function that will invoke the MCP tool
			var function = KernelFunctionFactory.CreateFromMethod(
				method: async (KernelArguments args) =>
				{
					var arguments = new Dictionary<string, object>();
					foreach (var param in parameters.Select(p => p.Name))
					{
						if (args.TryGetValue(param, out var value))
						{
							arguments[param] = value ?? string.Empty;
						}
					}

					return await CallToolAsync(tool.Name, arguments).ConfigureAwait(false);
				},
				parameters: parameters,
				functionName: tool.Name,
				description: tool.Description ?? $"Calls the {tool.Name} tool on the {Name} MCP server"
			);

			return function;
		}

		/// <summary>
		/// Sanitizes a plugin name to only include ASCII letters, digits, and underscores.
		/// </summary>
		/// <param name="name">The name to sanitize.</param>
		/// <returns>A sanitized name that is valid for Semantic Kernel plugins.</returns>
		private static string SanitizePluginName(string name)
		{
			var sb = new System.Text.StringBuilder();
			foreach (char c in name)
			{
				if (char.IsAsciiLetterOrDigit(c) || c == '_')
				{
					sb.Append(c);
				}
				else if (c == '-')
				{
					sb.Append('_');
				}
			}

			return sb.Length > 0 ? sb.ToString() : "mcp_plugin";
		}

		/// <summary>
		/// Disposes the MCP client and its resources.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (_mcpClient != null)
			{
				await _mcpClient.DisposeAsync().ConfigureAwait(false);
				_mcpClient = null;
			}

			_initLock.Dispose();
			_initialized = false;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Inner class that represents the MCP plugin with tool methods (used only when no tools are available).
		/// </summary>
		private sealed class McpPlugin
		{
			private readonly McpGatewayClient _client;

			public McpPlugin(McpGatewayClient client)
			{
				_client = client;
			}

			/// <summary>
			/// Gets information about the MCP server.
			/// </summary>
			/// <returns>Description of the MCP server capabilities.</returns>
			[KernelFunction, Description("Gets information about this MCP server's capabilities")]
			public string GetServerInfo()
			{
				return $"MCP Server: {_client.Name} - {_client.Description}";
			}
		}
	}
}
