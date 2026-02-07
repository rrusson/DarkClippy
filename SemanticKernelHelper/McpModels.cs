using System.Text.Json.Serialization;

namespace SemanticKernelHelper
{
	/// <summary>
	/// Represents a JSON-RPC 2.0 request.
	/// </summary>
	internal sealed class JsonRpcRequest
	{
		[JsonPropertyName("jsonrpc")]
		public string JsonRpc { get; set; } = "2.0";

		[JsonPropertyName("method")]
		public string Method { get; set; } = string.Empty;

		[JsonPropertyName("params")]
		public object? Params { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }
	}

	/// <summary>
	/// Represents a JSON-RPC 2.0 response.
	/// </summary>
	internal sealed class JsonRpcResponse
	{
		[JsonPropertyName("jsonrpc")]
		public string JsonRpc { get; set; } = string.Empty;

		[JsonPropertyName("result")]
		public object? Result { get; set; }

		[JsonPropertyName("error")]
		public JsonRpcError? Error { get; set; }

		[JsonPropertyName("id")]
		public int Id { get; set; }
	}

	/// <summary>
	/// Represents a JSON-RPC 2.0 error.
	/// </summary>
	internal sealed class JsonRpcError
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; } = string.Empty;

		[JsonPropertyName("data")]
		public object? Data { get; set; }
	}

	/// <summary>
	/// Represents the response from tools/list MCP method.
	/// </summary>
	internal sealed class ListToolsResult
	{
		[JsonPropertyName("tools")]
		public List<McpTool> Tools { get; set; } = [];
	}

	/// <summary>
	/// Represents an MCP tool definition.
	/// </summary>
	internal sealed class McpTool
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }

		[JsonPropertyName("inputSchema")]
		public McpInputSchema? InputSchema { get; set; }
	}

	/// <summary>
	/// Represents the JSON schema for tool input parameters.
	/// </summary>
	internal sealed class McpInputSchema
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;

		[JsonPropertyName("properties")]
		public Dictionary<string, McpParameter>? Properties { get; set; }

		[JsonPropertyName("required")]
		public List<string>? Required { get; set; }
	}

	/// <summary>
	/// Represents a parameter definition in the input schema.
	/// </summary>
	internal sealed class McpParameter
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;

		[JsonPropertyName("description")]
		public string? Description { get; set; }
	}

	/// <summary>
	/// Represents parameters for calling an MCP tool.
	/// </summary>
	internal sealed class CallToolParams
	{
		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("arguments")]
		public Dictionary<string, object>? Arguments { get; set; }
	}

	/// <summary>
	/// Represents the result of calling an MCP tool.
	/// </summary>
	internal sealed class CallToolResult
	{
		[JsonPropertyName("content")]
		public List<McpContent>? Content { get; set; }

		[JsonPropertyName("isError")]
		public bool IsError { get; set; }
	}

	/// <summary>
	/// Represents content returned from an MCP tool call.
	/// </summary>
	internal sealed class McpContent
	{
		[JsonPropertyName("type")]
		public string Type { get; set; } = string.Empty;

		[JsonPropertyName("text")]
		public string? Text { get; set; }
	}
}
