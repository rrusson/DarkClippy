using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SharedInterfaces;

using System.Text;

namespace SemanticKernelHelper
{
	public class SemanticKernelClient : IChatClient
	{
		private readonly ChatHistory _chatHistory = [];
		private readonly Kernel _kernel;
		private readonly IChatCompletionService _aiChatService;
		private int _exchangeCount;
		private readonly object _lock = new();
		private const int MaxExchangesBeforeFatigue = 10;

		/// <summary>
		/// Initializes a new instance of the SemanticKernelClient with the specified configuration.
		/// </summary>
		/// <param name="apiUrl">The base URL of the OpenAI-compatible API endpoint. Must be a valid URL.</param>
		/// <param name="model">The identifier of the model to use for chat completion.</param>
		/// <param name="apiKey">Optional API key for authentication. Defaults to empty string for local services like Ollama.</param>
		/// <exception cref="ArgumentNullException">Thrown when apiUrl or model is null.</exception>
		/// <exception cref="UriFormatException">Thrown when apiUrl is not a valid URL format.</exception>
		public SemanticKernelClient(string apiUrl, string model, string? apiKey = null)
		{
			if (apiUrl is null)
			{
				throw new ArgumentNullException(nameof(apiUrl));
			}

			if (model is null)
			{
				throw new ArgumentNullException(nameof(model));
			}

			if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri) ||
				(apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
			{
				throw new UriFormatException($"The value of {nameof(apiUrl)} is not a valid HTTP or HTTPS URL.");
			}

			_exchangeCount = 0;

			_kernel = Kernel.CreateBuilder()
				.AddOpenAIChatCompletion(
					modelId: model,
					endpoint: apiUri,
					apiKey: apiKey ?? string.Empty)
				.Build();

			_aiChatService = _kernel.GetRequiredService<IChatCompletionService>();

			string systemPrompt = "You are Dark Clippy, a profane talking paperclip. You give damn snarky, sarcastic, impatient, and insulting, short answers to questions. And you swear a lot.";
			_chatHistory.Add(new ChatMessageContent(AuthorRole.System, systemPrompt));
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

			await foreach (StreamingChatMessageContent item in _aiChatService.GetStreamingChatMessageContentsAsync(_chatHistory).ConfigureAwait(false))
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
	}
}
