using Newtonsoft.Json;

namespace LocalAiService
{
	public class ChatCompletionNonStreaming
	{
		[JsonProperty("id")]
		public string? Id { get; set; }

		[JsonProperty("object")]
		public string? ObjectType { get; set; }

		[JsonProperty("created")]
		public long? Created { get; set; }

		[JsonProperty("model")]
		public string? Model { get; set; }

		[JsonProperty("choices")]
		public List<OneChoice>? Choices { get; set; }

		[JsonProperty("usage")]
		public Usage? Usage { get; set; }
	}

	public class OneChoice
	{
		[JsonProperty("index")]
		public int Index { get; set; }

		[JsonProperty("message")]
		public Delta? Message { get; set; }

		[JsonProperty("finish_reason")]
		public string? FinishReason { get; set; }
	}

	public class Usage
	{
		[JsonProperty("prompt_tokens")]
		public int PromptTokens { get; set; }

		[JsonProperty("completion_tokens")]
		public int CompletionTokens { get; set; }

		[JsonProperty("total_tokens")]
		public int TotalTokens { get; set; }
	}
}
