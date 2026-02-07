using System.Text.Json.Serialization;

namespace SemanticKernelHelper
{
	/// <summary>
	/// Represents the response from Ollama's /api/show endpoint.
	/// </summary>
	internal sealed class OllamaShowResponse
	{
		[JsonPropertyName("modelfile")]
		public string? Modelfile { get; set; }

		[JsonPropertyName("parameters")]
		public string? Parameters { get; set; }

		[JsonPropertyName("template")]
		public string? Template { get; set; }

		[JsonPropertyName("details")]
		public OllamaModelDetails? Details { get; set; }

		[JsonPropertyName("model_info")]
		public Dictionary<string, object>? ModelInfo { get; set; }
	}

	/// <summary>
	/// Represents model details from Ollama.
	/// </summary>
	internal sealed class OllamaModelDetails
	{
		[JsonPropertyName("parent_model")]
		public string? ParentModel { get; set; }

		[JsonPropertyName("format")]
		public string? Format { get; set; }

		[JsonPropertyName("family")]
		public string? Family { get; set; }

		[JsonPropertyName("families")]
		public List<string>? Families { get; set; }

		[JsonPropertyName("parameter_size")]
		public string? ParameterSize { get; set; }

		[JsonPropertyName("quantization_level")]
		public string? QuantizationLevel { get; set; }
	}
}
