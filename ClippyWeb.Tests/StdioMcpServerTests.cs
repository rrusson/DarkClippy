using SemanticKernelHelper;

using SharedInterfaces;

namespace ClippyWeb.Tests
{
	[TestClass]
	public class StdioMcpServerTests
	{
		[TestMethod]
		public void WhenConfigurationProvidedThenServerIsCreated()
		{
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = true,
				Command = "echo",
				ServerType = "stdio"
			};

			var server = new StdioMcpServer(config);

			Assert.IsNotNull(server);
			Assert.AreEqual("test-server", server.Name);
			Assert.AreEqual("Test MCP Server", server.Description);
			Assert.IsTrue(server.IsEnabled);
		}

		[TestMethod]
		public void WhenDisabledInConfigThenServerIsDisabled()
		{
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = false,
				Command = "echo",
				ServerType = "stdio"
			};

			var server = new StdioMcpServer(config);

			Assert.IsFalse(server.IsEnabled);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void WhenNullConfigurationThenThrowsException()
		{
			_ = new StdioMcpServer(null!);
		}

		[TestMethod]
		public void WhenCreatePluginCalledThenPluginIsReturned()
		{
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = true,
				Command = "echo",
				ServerType = "stdio"
			};

			var server = new StdioMcpServer(config);
			var plugin = server.CreatePlugin();

			Assert.IsNotNull(plugin);
			// Plugin name should be sanitized (dashes replaced with underscores)
			Assert.AreEqual("test_server", plugin.Name);
		}
	}
}
