using Newtonsoft.Json;

using SharedInterfaces;

using System.Text;

namespace LocalAiService
{
	/// <summary>
	/// This LLM ChatClient works with models hosted in LM Studio, etc.
	/// </summary>
	public class ChatClient(string apiUrl, string model) : IChatClient
	{
		private static readonly string[] _separator = ["\n"];
		private readonly string _apiUrl = apiUrl + "/chat/completions";
		private readonly string _model = model;

		public async Task<string?> GetChatResponseAsync(string chatMessage)
		{
			var httpClient = new HttpClient();

			var requestBody = new
			{
				model = _model,
				messages = new[]
				{
					new { role = "system", content = "You are Dark Clippy, a profane talking paperclip. You give damn snarky, sarcastic, impatient, and insulting, but correct, short answers to questions. And you swear a lot." },
					new { role = "user", content = chatMessage }
								},
				temperature = 0.7,
				max_tokens = -1,
				stream = false
			};

			string json = JsonConvert.SerializeObject(requestBody);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			HttpResponseMessage response = await httpClient.PostAsync(_apiUrl, content).ConfigureAwait(false);
			string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			return ParseResponse(responseContent);
		}

		// For simple, non-streaming completions
		private static string? ParseResponse(string responseText)
		{
			if (responseText == null)
			{
				return null;
			}

			ChatCompletionNonStreaming? completionChunk = JsonConvert.DeserializeObject<ChatCompletionNonStreaming>(responseText);
			string? content = completionChunk?.Choices?.FirstOrDefault()?.Message?.Content;

			return content;
		}

		// For streaming completions (Note: this isn't used since it's impractical to send each word over HTTP)
		private static IEnumerable<string?> ParseResponseStreaming(string responseText)
		{
			if (string.IsNullOrEmpty(responseText))
			{
				yield return null;
			}

			string[] lines = responseText.Split(_separator, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (!line.StartsWith("data: "))
				{
					continue;
				}

				var json = line[6..]; // Remove "data: "
				if (json == "[DONE]")
				{
					break;
				}

				ChatCompletionStreaming? completionChunk = JsonConvert.DeserializeObject<ChatCompletionStreaming>(json);
				string? content = completionChunk?.Choices?.FirstOrDefault()?.Delta?.Content;

				if (!string.IsNullOrEmpty(content))
				{
					yield return content;
				}
			}
		}
	}
}
