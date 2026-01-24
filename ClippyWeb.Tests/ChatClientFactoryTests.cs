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
		public void IfSessionKeyIsProvidedThenClientIsReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client = factory.GetOrCreateClient("test-session");

			Assert.IsNotNull(client);
		}

		[TestMethod]
		public void IfSameSessionKeyIsUsedThenSameClientIsReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client1 = factory.GetOrCreateClient("test-session");
			var client2 = factory.GetOrCreateClient("test-session");

			Assert.AreSame(client1, client2);
		}

		[TestMethod]
		public void IfDifferentSessionKeysAreUsedThenDifferentClientsAreReturned()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);

			var client1 = factory.GetOrCreateClient("session-1");
			var client2 = factory.GetOrCreateClient("session-2");

			Assert.AreNotSame(client1, client2);
		}

		[TestMethod]
		public void IfMultipleSessionsAreConcurrentThenFactoryIsThreadSafe()
		{
			var factory = new ChatClientFactory(TestApiUrl, TestModel, TestApiKey, _cache);
			var clients = new List<IChatClient>();
			var lockObj = new object();

			Parallel.For(0, 10, i =>
			{
				var client = factory.GetOrCreateClient($"session-{i % 3}");
				lock (lockObj)
				{
					clients.Add(client);
				}
			});

			Assert.AreEqual(10, clients.Count);
		}
	}
}
