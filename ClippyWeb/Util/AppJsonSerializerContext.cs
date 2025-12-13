using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ClippyWeb.Util
{
	/// <summary>
	/// Defines a JSON serializer context to support System.Text.Json serialization
	/// </summary>
	/// <remarks>
	/// This helps with the OpenAI client library's serialization needs.
	/// The compiler generates the implementation for this partial class.
	/// </remarks>
	[ExcludeFromCodeCoverage]
	[JsonSerializable(typeof(object))]  // Making it handle generic objects
	[JsonSerializable(typeof(string))]  // Common type used in serialization
	[JsonSerializable(typeof(Dictionary<string, object>))]  // Common for API requests
	[JsonSerializable(typeof(List<object>))]
	public partial class AppJsonSerializerContext : JsonSerializerContext
	{
	}
}
