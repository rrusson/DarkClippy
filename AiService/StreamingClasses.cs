using Newtonsoft.Json;

namespace LocalAiService
{
	// Response returns multiple lines of JSON data in the following format:
	//data: {"id":"chatcmpl-4brrx6fp2d24eanfloznqm","object":"chat.completion.chunk","created":1729039972,"model":"lmstudio-ai/gemma-2b-it-GGUF/gemma-2b-it-q8_0.gguf","choices":[{"index":0,"delta":{"role":"assistant","content":" intelligent"},"finish_reason":null}]}

	public class ChatCompletionStreaming
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
		public List<Choice>? Choices { get; set; }
	}

	public class CompletionChunk
	{
		[JsonProperty("choices")]
		public List<Choice>? Choices { get; set; }
	}

	public class Choice
	{
		[JsonProperty("index")]
		public int Index { get; set; }

		[JsonProperty("delta")]
		public Delta? Delta { get; set; }

		[JsonProperty("finish_reason")]
		public string? FinishReason { get; set; }
	}

	public class Delta
	{
		[JsonProperty("role")]
		public string? Role { get; set; }

		[JsonProperty("content")]
		public string? Content { get; set; }
	}
}
