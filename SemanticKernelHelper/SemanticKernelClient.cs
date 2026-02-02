using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SharedInterfaces;

namespace SemanticKernelHelper
{
	public class SemanticKernelClient : IChatClient
	{
		private const string _systemPrompt = "You are Dark Clippy, a profane talking paperclip. You give damn snarky, sarcastic, impatient, and insulting, short answers to questions. And you swear a lot.";
		private readonly ChatHistory _chatHistory = [];
		private readonly IChatCompletionService _aiChatService;
		private readonly Kernel _kernel;
		private int _exchangeCount;
		private readonly object _lock = new();
		private const int MaxExchangesBeforeFatigue = 10;
		private readonly bool _supportsTools;

		/// <summary>
		/// Initializes a new instance of the SemanticKernelClient with the specified configuration.
		/// </summary>
		/// <param name="apiUrl">The base URL of the OpenAI-compatible API endpoint. Must be a valid URL.</param>
		/// <param name="model">The identifier of the model to use for chat completion.</param>
		/// <param name="apiKey">Optional API key for authentication. Defaults to empty string for local services like Ollama.</param>
		/// <param name="mcpServers">Optional collection of MCP servers to integrate as plugins.</param>
		/// <param name="capabilityDetector">Optional capability detector to check if the model supports tools.</param>
		/// <param name="logger">Optional logger for diagnostic information.</param>
		/// <exception cref="ArgumentNullException">Thrown when apiUrl or model is null.</exception>
		/// <exception cref="UriFormatException">Thrown when apiUrl is not a valid URL format.</exception>
		public SemanticKernelClient(
			string apiUrl,
			string model,
			string? apiKey = null,
			IEnumerable<StdioMcpServer>? mcpServers = null,
			IModelCapabilityDetector? capabilityDetector = null,
			ILogger? logger = null)
		{
			ArgumentNullException.ThrowIfNull(apiUrl);
			ArgumentNullException.ThrowIfNull(model);

			if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri) || (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
			{
				throw new UriFormatException($"The value of {nameof(apiUrl)} is not a valid HTTP or HTTPS URL.");
			}

			_exchangeCount = 0;

			// Check if model supports tools
			_supportsTools = capabilityDetector?.SupportsToolsAsync(model).GetAwaiter().GetResult() ?? false;

			if (!_supportsTools)
			{
				logger?.LogWarning("Model '{Model}' does not support tools/function calling - MCP plugins will be disabled", model);
			}

			_kernel = Kernel.CreateBuilder()
				.AddOpenAIChatCompletion(
					modelId: model,
					endpoint: apiUri,
					apiKey: apiKey ?? string.Empty)
				.Build();

			_aiChatService = _kernel.GetRequiredService<IChatCompletionService>();

			_chatHistory.Add(new ChatMessageContent(AuthorRole.System, _systemPrompt));

			if (_supportsTools)
			{
				RegisterMcpServers(mcpServers, logger);
			}
			else if (mcpServers?.Any() == true)
			{
				logger?.LogInformation("Skipping registration of {Count} MCP server(s) because model does not support tools", mcpServers.Count());
			}
		}

		/// <summary>
		/// Generates a chat response from the AI assistant based on the provided user message, maintaining conversational context.
		/// </summary>
		/// <param name="chatMessage">The user's message to send to Dark Clippy. Cannot be null or empty.</param>
		/// <returns>A string containing Dark Clippy's entire response to the user message</returns>
		/// <remarks>The conversation history is preserved across calls to provide contextually relevant responses. This method is thread-safe.</remarks>
		public async Task<string?> GetChatResponseAsync(string chatMessage)
		{
			if (string.IsNullOrWhiteSpace(chatMessage))
			{
				return "You say something?";
			}

			lock (_lock)
			{
				_exchangeCount++;

				if (_exchangeCount > MaxExchangesBeforeFatigue)
				{
					return "Alright, I'm sick of talking about this shit. Go bother someone else.";
				}

				_chatHistory.Add(new ChatMessageContent(AuthorRole.User, chatMessage));
			}

			var responseBuilder = new StringBuilder();

			// Only enable function calling if the model supports tools
			var executionSettings = _supportsTools
				? new PromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }
				: null;

			await foreach (StreamingChatMessageContent item in _aiChatService.GetStreamingChatMessageContentsAsync(_chatHistory, executionSettings, _kernel).ConfigureAwait(false))
			{
				responseBuilder.Append(item.Content);
			}

			string response = responseBuilder.ToString();

			lock (_lock)
			{
				_chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));
			}

			return response;
		}

		// Add MCP server plugins if provided
		private void RegisterMcpServers(IEnumerable<StdioMcpServer>? mcpServers, ILogger? logger)
		{
			if (mcpServers == null)
			{
				return;
			}

			foreach (var mcpServer in mcpServers)
			{
				try
				{
					var plugin = mcpServer.CreatePlugin();
					_kernel.Plugins.Add(plugin);
					logger?.LogInformation("MCP plugin '{PluginName}' added successfully", mcpServer.Name);
				}
				catch (Exception ex)
				{
					logger?.LogWarning(ex, "Failed to add MCP plugin '{PluginName}', skipping", mcpServer.Name);
				}
			}
		}
	}
}
