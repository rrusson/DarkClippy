using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

using SharedInterfaces;

namespace SemanticKernelHelper
{
	/// <summary>
	/// Simplified MCP server implementation using stdio process communication.
	/// </summary>
	public class StdioMcpServer : IMcpServer, IAsyncDisposable
	{
		private readonly McpServerConfiguration _configuration;
		private readonly ILogger? _logger;
		private Process? _process;
		private bool _initialized;
		private readonly object _lock = new();

		/// <summary>
		/// Initializes a new instance of the StdioMcpServer class.
		/// </summary>
		/// <param name="configuration">The configuration for this MCP server.</param>
		/// <param name="logger">Optional logger for diagnostic information.</param>
		public StdioMcpServer(McpServerConfiguration configuration, ILogger? logger = null)
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
		/// Initializes the MCP server connection.
		/// </summary>
		/// <returns>A task that represents the asynchronous initialization operation.</returns>
		public async Task InitializeAsync()
		{
			if (_initialized)
			{
				return;
			}

			if (string.IsNullOrEmpty(_configuration.Command))
			{
				throw new InvalidOperationException($"Command is required for stdio MCP server '{Name}'");
			}

			try
			{
				_logger?.LogInformation("Initializing MCP server: {Name}", Name);

				var startInfo = new ProcessStartInfo
				{
					FileName = _configuration.Command,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true
				};

				if (_configuration.Arguments != null)
				{
					foreach (var arg in _configuration.Arguments)
					{
						startInfo.ArgumentList.Add(arg);
					}
				}

				if (_configuration.EnvironmentVariables != null)
				{
					foreach (var kvp in _configuration.EnvironmentVariables)
					{
						startInfo.Environment[kvp.Key] = kvp.Value;
					}
				}

				_process = Process.Start(startInfo);

				if (_process == null)
				{
					throw new InvalidOperationException($"Failed to start process for MCP server '{Name}'");
				}

				_initialized = true;
				_logger?.LogInformation("MCP server '{Name}' initialized successfully", Name);
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to initialize MCP server '{Name}'", Name);
				throw;
			}
		}

		/// <summary>
		/// Gets the available tools from this MCP server.
		/// </summary>
		/// <returns>A collection of tool definitions available from this server.</returns>
		public Task<IEnumerable<object>> GetToolsAsync()
		{
			if (!_initialized)
			{
				throw new InvalidOperationException($"MCP server '{Name}' is not initialized. Call InitializeAsync first.");
			}

			return Task.FromResult<IEnumerable<object>>([]);
		}

		/// <summary>
		/// Creates a Semantic Kernel plugin from this MCP server.
		/// </summary>
		/// <returns>A kernel plugin that can be added to Semantic Kernel.</returns>
		public KernelPlugin CreatePlugin()
		{
			// Sanitize the plugin name - Semantic Kernel only allows ASCII letters, digits, and underscores
			string sanitizedName = SanitizePluginName(Name);
			return KernelPluginFactory.CreateFromObject(new McpPlugin(this, _logger), sanitizedName);
		}

		/// <summary>
		/// Sanitizes a plugin name to only include ASCII letters, digits, and underscores.
		/// </summary>
		/// <param name="name">The name to sanitize.</param>
		/// <returns>A sanitized name that is valid for Semantic Kernel plugins.</returns>
		private static string SanitizePluginName(string name)
		{
			var sb = new StringBuilder();
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
		/// Sends a request to the MCP server and gets a response.
		/// </summary>
		/// <param name="request">The JSON-RPC request to send.</param>
		/// <returns>The response from the MCP server.</returns>
		private async Task<string> SendRequestAsync(string request)
		{
			if (_process == null || _process.HasExited)
			{
				throw new InvalidOperationException($"MCP server process '{Name}' is not running");
			}

			lock (_lock)
			{
				_process.StandardInput.WriteLine(request);
				_process.StandardInput.Flush();
			}

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
			var response = await _process.StandardOutput.ReadLineAsync(cts.Token).ConfigureAwait(false);

			return response ?? string.Empty;
		}

		/// <summary>
		/// Disposes the MCP server and its resources.
		/// </summary>
		public async ValueTask DisposeAsync()
		{
			if (_process != null)
			{
				try
				{
					if (!_process.HasExited)
					{
						_process.Kill(true);
						await _process.WaitForExitAsync().ConfigureAwait(false);
					}

					_process.Dispose();
					_process = null;
				}
				catch (Exception ex)
				{
					_logger?.LogWarning(ex, "Error disposing MCP server '{Name}'", Name);
				}
			}

			_initialized = false;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Inner class that represents the MCP plugin with tool methods.
		/// </summary>
		private class McpPlugin
		{
			private readonly StdioMcpServer _server;
			private readonly ILogger? _logger;

			public McpPlugin(StdioMcpServer server, ILogger? logger)
			{
				_server = server;
				_logger = logger;
			}

			/// <summary>
			/// Gets information about the MCP server.
			/// </summary>
			/// <returns>Description of the MCP server capabilities.</returns>
			[KernelFunction, Description("Gets information about this MCP server's capabilities")]
			public string GetServerInfo()
			{
				return $"MCP Server: {_server.Name} - {_server.Description}";
			}
		}
	}
}
