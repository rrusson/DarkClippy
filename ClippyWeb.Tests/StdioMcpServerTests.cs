using System.ComponentModel;

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
		public void WhenNullConfigurationThenThrowsException()
		{
			Assert.ThrowsExactly<ArgumentNullException>(() => _ = new StdioMcpServer(null!));
		}

		[TestMethod]
		public async Task WhenInvalidCommandThenInitializeThrowsException()
		{
			var config = new McpServerConfiguration
			{
				Name = "test-server",
				Description = "Test MCP Server",
				Enabled = true,
				Command = "invalid-command-that-does-not-exist",
				ServerType = "stdio"
			};

			var server = new StdioMcpServer(config);
			await Assert.ThrowsExactlyAsync<Win32Exception>(async () => await server.InitializeAsync());
		}

		// TODO: This test needs to be refactored to properly test CreatePlugin functionality.
		// Current limitation: StdioMcpServer requires a real MCP server process, which makes
		// unit testing difficult. Consider:
		// 1. Creating an integration test with a real MCP server
		// 2. Refactoring StdioMcpServer to accept a process factory for better testability
		// 3. Using a test double or creating a mock MCP server process
		//
		[TestMethod]
		[Ignore("Requires a valid MCP server process to test CreatePlugin functionality.")]
		public async Task WhenCreatePluginCalledThenPluginIsReturned()
		{
			// This test would require a valid MCP server command
			// For now, it's commented out until proper mocking infrastructure is in place
		}
	}
}
