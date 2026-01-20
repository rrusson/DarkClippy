using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using SemanticKernelHelper;

namespace ClippyWeb.Tests
{
	[TestClass]
	public class SemanticKernelClientTests
	{
		private const string TestApiUrl = "http://localhost:11434/v1";
		private const string TestModel = "test-model";
		private const string TestApiKey = "test-api-key";

		[TestMethod]
		public void IfApiKeyIsProvidedThenClientIsCreated()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel, TestApiKey);

			Assert.IsNotNull(client);
		}

		[TestMethod]
		public void IfApiKeyIsNullThenClientIsCreatedWithEmptyKey()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel, null);

			Assert.IsNotNull(client);
		}

		[TestMethod]
		public void IfApiKeyIsNotProvidedThenClientIsCreatedWithEmptyKey()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel);

			Assert.IsNotNull(client);
		}

		[TestMethod]
		public async Task IfEmptyMessageThenReturnsDefaultResponse()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel, TestApiKey);

			var response = await client.GetChatResponseAsync("");

			Assert.AreEqual("You say something?", response);
		}

		[TestMethod]
		public async Task IfWhitespaceMessageThenReturnsDefaultResponse()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel, TestApiKey);

			var response = await client.GetChatResponseAsync("   ");

			Assert.AreEqual("You say something?", response);
		}

		[TestMethod]
		public async Task IfNullMessageThenReturnsDefaultResponse()
		{
			var client = new SemanticKernelClient(TestApiUrl, TestModel, TestApiKey);

			var response = await client.GetChatResponseAsync(null!);

			Assert.AreEqual("You say something?", response);
		}
	}
}
