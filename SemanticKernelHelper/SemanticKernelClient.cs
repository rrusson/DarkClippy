using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedInterfaces;
using System.Text;

namespace SemanticKernelHelper
{
	public class SemanticKernelClient(string apiUrl, string model) : IChatClient
	{
		private readonly string _apiUrl = apiUrl;
		private readonly string _model = model;
		private readonly ChatHistory _chatHistory = [];

		public async Task<string?> GetChatResponseAsync(string chatMessage)
		{
			// Create a kernel with OpenAI chat completion
			Kernel kernel = Kernel.CreateBuilder()
							.AddOpenAIChatCompletion(
								modelId: _model,
								endpoint: new Uri(_apiUrl),
								apiKey: "")
							.Build();

			IChatCompletionService aiChatService = kernel.GetRequiredService<IChatCompletionService>();

			string systemPrompt = "You are Dark Clippy, a profane talking paperclip. You give damn snarky, sarcastic, impatient, and insulting, short answers to questions. And you swear a lot.";
			_chatHistory.Add(new ChatMessageContent(AuthorRole.System, systemPrompt));

			// Consider persisting the chat history for more coherent conversations
			_chatHistory.Add(new ChatMessageContent(AuthorRole.User, chatMessage));

			var responseBuilder = new StringBuilder();

			await foreach (StreamingChatMessageContent item in aiChatService.GetStreamingChatMessageContentsAsync(_chatHistory))
			{
				responseBuilder.Append(item.Content);
			}

			string response = responseBuilder.ToString();

			_chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));

			return response;
		}
	}
}
