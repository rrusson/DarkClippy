using Microsoft.Extensions.Caching.Memory;

using SemanticKernelHelper;

using SharedInterfaces;

namespace ClippyWeb.Tests
{
	[TestClass]
	public class ChatClientFactoryTests
	{
		private const string TestApiUrl = "http://localhost:11434/v1";
		private const string TestModel = "test-model";
		private const string TestApiKey = "test-api-key";
		private IMemoryCache _cache = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_cache = new MemoryCache(new MemoryCacheOptions());
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_cache?.Dispose();
		}

		[TestMethod]
		public async Task IfSessionKeyIsProvidedThenClientIsReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client = await factory.GetOrCreateClientAsync("test-session");

			Assert.IsNotNull(client);
		}

		[TestMethod]
		public async Task IfSameSessionKeyIsUsedThenSameClientIsReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client1 = await factory.GetOrCreateClientAsync("test-session");
			var client2 = await factory.GetOrCreateClientAsync("test-session");

			Assert.AreSame(client1, client2);
		}

		[TestMethod]
		public async Task IfDifferentSessionKeysAreUsedThenDifferentClientsAreReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client1 = await factory.GetOrCreateClientAsync("session-1");
			var client2 = await factory.GetOrCreateClientAsync("session-2");

			Assert.AreNotSame(client1, client2);
		}

		[TestMethod]
		public async Task IfMultipleSessionsAreConcurrentThenFactoryIsThreadSafe()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);
			var clients = new List<IChatClient>();
			var lockObj = new object();

			Parallel.For(0, 10, async i =>
			{
				var client = await factory.GetOrCreateClientAsync($"session-{i % 3}");
				lock (lockObj)
				{
					clients.Add(client);
				}
			});

			Assert.AreEqual(10, clients.Count);
		}
	}
}
