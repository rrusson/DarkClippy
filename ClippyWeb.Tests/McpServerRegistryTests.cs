using SemanticKernelHelper;

using SharedInterfaces;

namespace ClippyWeb.Tests
{
	[TestClass]
	public class McpServerRegistryTests
	{
		[TestMethod]
		public void WhenNoServersRegisteredThenGetAllReturnsEmpty()
		{
			var registry = new McpServerRegistry();
			var servers = registry.GetAll();

			Assert.IsNotNull(servers);
			Assert.AreEqual(0, servers.Count());
		}

		[TestMethod]
		public void WhenServerRegisteredThenGetAllReturnsServer()
		{
			var registry = new McpServerRegistry();
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = true,
				Command = "test",
				ServerType = "stdio"
			};
			var server = new StdioMcpServer(config);

			registry.Register(server);
			var servers = registry.GetAll();

			Assert.AreEqual(1, servers.Count());
			Assert.AreEqual("test-server", servers.First().Name);
		}

		[TestMethod]
		public void WhenEnabledServerRegisteredThenGetEnabledReturnsServer()
		{
			var registry = new McpServerRegistry();
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = true,
				Command = "test",
				ServerType = "stdio"
			};
			var server = new StdioMcpServer(config);

			registry.Register(server);
			var enabledServers = registry.GetEnabled();

			Assert.AreEqual(1, enabledServers.Count());
		}

		[TestMethod]
		public void WhenDisabledServerRegisteredThenGetEnabledReturnsEmpty()
		{
			var registry = new McpServerRegistry();
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = false,
				Command = "test",
				ServerType = "stdio"
			};
			var server = new StdioMcpServer(config);

			registry.Register(server);
			var enabledServers = registry.GetEnabled();

			Assert.AreEqual(0, enabledServers.Count());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void WhenNullServerRegisteredThenThrowsException()
		{
			var registry = new McpServerRegistry();

			registry.Register(null!);
		}
	}
}
