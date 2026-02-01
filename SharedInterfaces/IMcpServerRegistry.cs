namespace SharedInterfaces
{
	/// <summary>
	/// Registry for managing multiple MCP servers.
	/// </summary>
	public interface IMcpServerRegistry
	{
		/// <summary>
		/// Gets all registered MCP servers.
		/// </summary>
		/// <returns>A collection of all registered MCP servers.</returns>
		IEnumerable<IMcpServer> GetAll();

		/// <summary>
		/// Gets all enabled MCP servers.
		/// </summary>
		/// <returns>A collection of enabled MCP servers.</returns>
		IEnumerable<IMcpServer> GetEnabled();

		/// <summary>
		/// Registers an MCP server.
		/// </summary>
		/// <param name="server">The MCP server to register.</param>
		void Register(IMcpServer server);
	}
}
