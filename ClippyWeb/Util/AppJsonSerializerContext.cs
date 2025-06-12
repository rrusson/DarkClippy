using System.Text.Json.Serialization;

namespace ClippyWeb.Util
{
	// Defines a JSON serializer context to support System.Text.Json serialization
	// This helps with the OpenAI client library's serialization needs
	[JsonSerializable(typeof(object))]  // Making it handle generic objects
	[JsonSerializable(typeof(string))]  // Common type used in serialization
	[JsonSerializable(typeof(Dictionary<string, object>))]  // Common for API requests
	[JsonSerializable(typeof(List<object>))]
	public partial class AppJsonSerializerContext : JsonSerializerContext
	{
	}
}
