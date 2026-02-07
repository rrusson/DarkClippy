using SharedInterfaces;

namespace SemanticKernelHelper
{
	/// <summary>
	/// Registry for managing MCP servers.
	/// </summary>
	public class McpServerRegistry : IMcpServerRegistry
	{
		private readonly List<IMcpServer> _servers = [];
		private readonly object _lock = new();

		/// <summary>
		/// Gets all registered MCP servers.
		/// </summary>
		/// <returns>A collection of all registered MCP servers.</returns>
		public IEnumerable<IMcpServer> GetAll()
		{
			lock (_lock)
			{
				return _servers.ToList();
			}
		}

		/// <summary>
		/// Gets all enabled MCP servers.
		/// </summary>
		/// <returns>A collection of enabled MCP servers.</returns>
		public IEnumerable<IMcpServer> GetEnabled()
		{
			lock (_lock)
			{
				return _servers.Where(s => s.IsEnabled).ToList();
			}
		}

		/// <summary>
		/// Registers an MCP server.
		/// </summary>
		/// <param name="server">The MCP server to register.</param>
		public void Register(IMcpServer server)
		{
			ArgumentNullException.ThrowIfNull(server);

			lock (_lock)
			{
				_servers.Add(server);
			}
		}
	}
}
