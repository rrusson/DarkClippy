using Microsoft.SemanticKernel;

namespace SharedInterfaces
{
	/// <summary>
	/// Represents an MCP (Model Context Protocol) server that provides tools to AI agents.
	/// </summary>
	public interface IMcpServer
	{
		/// <summary>
		/// Gets the unique identifier for this MCP server.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets a description of the MCP server's capabilities.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets a value indicating whether this MCP server is enabled.
		/// </summary>
		bool IsEnabled { get; }

		/// <summary>
		/// Initializes the MCP server connection.
		/// </summary>
		/// <returns>A task that represents the asynchronous initialization operation.</returns>
		Task InitializeAsync();

		/// <summary>
		/// Gets the available tools from this MCP server.
		/// </summary>
		/// <returns>A collection of tool definitions available from this server.</returns>
		Task<IEnumerable<object>> GetToolsAsync();


		Microsoft.SemanticKernel.KernelPlugin CreatePlugin();
	}
}
