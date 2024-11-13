using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SharedInterfaces;

using System.Text;
#pragma warning disable SKEXP0010  // Everything is impermanent, isn't it?                                          

namespace SemanticKernelHelper
{
	public class SemanticKernalClient(string apiUrl, string model) : IChatClient
	{
		private readonly string _apiUrl = apiUrl;
		private readonly string _model = model;
		private readonly ChatHistory _chatHistory = [];

		public async Task<string?> GetChatResponseAsync(string chatMessage)
		{
			// Create a kernel with OpenAI chat completion
			Kernel kernel = Kernel.CreateBuilder()
							.AddOpenAIChatCompletion(
								//modelId: "phi3:mini",
								//modelId: "hf.co/SanctumAI/Meta-Llama-3.1-8B-Instruct-GGUF:Q2_K",
								//modelId: "HammerAI/neuraldaredevil-abliterated",
								modelId: _model,
								endpoint: new Uri(_apiUrl),
								apiKey: "")
							.Build();

			IChatCompletionService aiChatService = kernel.GetRequiredService<IChatCompletionService>();

			string systemPrompt = "You are Dark Clippy, a profane talking paperclip. You give damn snarky, sarcastic, impatient, and insulting, but correct, short answers to questions. And you swear a lot.";
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
