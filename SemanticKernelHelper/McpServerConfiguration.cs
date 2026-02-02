namespace SemanticKernelHelper
{
	/// <summary>
	/// Configuration for an MCP server.
	/// </summary>
	public class McpServerConfiguration
	{
		/// <summary>
		/// Gets or sets the name of the MCP server.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the description of the MCP server.
		/// </summary>
		public string Description { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the server type (e.g., "stdio", "http", "tcp").
		/// </summary>
		public string ServerType { get; set; } = "stdio";

		/// <summary>
		/// Gets or sets the command to execute for stdio-based servers.
		/// </summary>
		public string? Command { get; set; }

		/// <summary>
		/// Gets or sets the arguments for the command.
		/// </summary>
		public string[]? Arguments { get; set; }

		/// <summary>
		/// Gets or sets the environment variables for the server process.
		/// </summary>
		public Dictionary<string, string>? EnvironmentVariables { get; set; }

		/// <summary>
		/// Gets or sets the working directory for the server process.
		/// </summary>
		public string? WorkingDirectory { get; set; }

		/// <summary>
		/// Gets or sets the endpoint URL for HTTP-based servers.
		/// </summary>
		public string? Endpoint { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this server is enabled.
		/// </summary>
		public bool Enabled { get; set; } = true;
	}
}
