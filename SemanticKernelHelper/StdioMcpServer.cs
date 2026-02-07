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
		private readonly SemaphoreSlim _stdinWriteLock = new(1, 1);
		private int _requestId;
		private List<McpTool>? _cachedTools;

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

				// Resolve command for Windows (npx requires .cmd extension when UseShellExecute = false)
				string command = _configuration.Command;
				if (OperatingSystem.IsWindows() && 
					(command.Equals("npx", StringComparison.OrdinalIgnoreCase) || 
					 command.Equals("npm", StringComparison.OrdinalIgnoreCase)))
				{
					command += ".cmd";
				}

				var startInfo = new ProcessStartInfo
				{
					FileName = command,
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

				if (!string.IsNullOrEmpty(_configuration.WorkingDirectory))
				{
					startInfo.WorkingDirectory = _configuration.WorkingDirectory;
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
		public async Task<IEnumerable<object>> GetToolsAsync()
		{
			if (!_initialized)
			{
				throw new InvalidOperationException($"MCP server '{Name}' is not initialized. Call InitializeAsync first.");
			}

			if (_cachedTools != null)
			{
				return _cachedTools;
			}

			try
			{
				var request = new JsonRpcRequest
				{
					Method = "tools/list",
					Id = Interlocked.Increment(ref _requestId)
				};

				var responseJson = await SendRequestAsync(JsonSerializer.Serialize(request)).ConfigureAwait(false);
				var response = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson);

				if (response?.Error != null)
				{
					_logger?.LogError("MCP server '{Name}' returned error: {ErrorMessage}", Name, response.Error.Message);
					return [];
				}

				if (response?.Result != null)
				{
					var resultJson = JsonSerializer.Serialize(response.Result);
					var listResult = JsonSerializer.Deserialize<ListToolsResult>(resultJson);
					_cachedTools = listResult?.Tools ?? [];
					_logger?.LogInformation("MCP server '{Name}' returned {ToolCount} tools", Name, _cachedTools.Count);
					return _cachedTools;
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to get tools from MCP server '{Name}'", Name);
			}

			return [];
		}

		/// <summary>
		/// Calls a tool on the MCP server.
		/// </summary>
		/// <param name="toolName">The name of the tool to call.</param>
		/// <param name="arguments">The arguments to pass to the tool.</param>
		/// <returns>The result from the tool call.</returns>
		public async Task<string> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null)
		{
			if (!_initialized)
			{
				throw new InvalidOperationException($"MCP server '{Name}' is not initialized. Call InitializeAsync first.");
			}

			try
			{
				var request = new JsonRpcRequest
				{
					Method = "tools/call",
					Params = new CallToolParams
					{
						Name = toolName,
						Arguments = arguments
					},
					Id = Interlocked.Increment(ref _requestId)
				};

				var responseJson = await SendRequestAsync(JsonSerializer.Serialize(request)).ConfigureAwait(false);
				var response = JsonSerializer.Deserialize<JsonRpcResponse>(responseJson);

				if (response?.Error != null)
				{
					_logger?.LogError("MCP tool '{ToolName}' on server '{ServerName}' returned error: {ErrorMessage}", toolName, Name, response.Error.Message);
					return $"Error calling tool: {response.Error.Message}";
				}

				if (response?.Result != null)
				{
					var resultJson = JsonSerializer.Serialize(response.Result);
					var callResult = JsonSerializer.Deserialize<CallToolResult>(resultJson);

					if (callResult?.IsError == true)
					{
						var errorText = callResult.Content?.FirstOrDefault()?.Text ?? "Unknown error";
						_logger?.LogError("MCP tool '{ToolName}' on server '{ServerName}' failed: {Error}", toolName, Name, errorText);
						return $"Tool error: {errorText}";
					}

					var resultText = string.Join("\n", callResult?.Content?.Select(c => c.Text ?? string.Empty) ?? []);
					_logger?.LogInformation("MCP tool '{ToolName}' on server '{ServerName}' completed successfully", toolName, Name);
					return resultText;
				}
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Failed to call MCP tool '{ToolName}' on server '{ServerName}'", toolName, Name);
				return $"Exception calling tool: {ex.Message}";
			}

			return "No result returned from tool";
		}

		/// <summary>
		/// Creates a Semantic Kernel plugin from this MCP server.
		/// </summary>
		/// <returns>A kernel plugin that can be added to Semantic Kernel.</returns>
		public async Task<KernelPlugin> CreatePluginAsync()
		{
			// Get tools from the MCP server
			var tools = (await GetToolsAsync().ConfigureAwait(false)).OfType<McpTool>().ToList();

			if (tools.Count == 0)
			{
				_logger?.LogWarning("MCP server '{Name}' has no tools available", Name);
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
					_logger?.LogWarning(ex, "Failed to create kernel function for tool '{ToolName}' on server '{ServerName}'", tool.Name, Name);
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
		private KernelFunction CreateKernelFunctionForTool(McpTool tool)
		{
			// Create parameters for the function
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

			await _stdinWriteLock.WaitAsync().ConfigureAwait(false);
			try
			{
				await _process.StandardInput.WriteLineAsync(request).ConfigureAwait(false);
				await _process.StandardInput.FlushAsync().ConfigureAwait(false);
			}
			finally
			{
				_stdinWriteLock.Release();
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

			_stdinWriteLock.Dispose();
			_initialized = false;
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Inner class that represents the MCP plugin with tool methods (used only when no tools are available).
		/// </summary>
		private sealed class McpPlugin
		{
			private readonly StdioMcpServer _server;

			public McpPlugin(StdioMcpServer server)
			{
				_server = server;
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
