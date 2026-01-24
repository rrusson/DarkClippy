using Microsoft.Extensions.Caching.Memory;

using SharedInterfaces;

namespace SemanticKernelHelper
{
	public class ChatClientFactory : IChatClientFactory
	{
		private readonly string _serviceUrl;
		private readonly string _model;
		private readonly string _apiKey;
		private readonly IMemoryCache _cache;
		private readonly object _lock = new();

		public ChatClientFactory(string serviceUrl, string model, string apiKey, IMemoryCache cache)
		{
			_serviceUrl = serviceUrl;
			_model = model;
			_apiKey = apiKey;
			_cache = cache;
		}

		public IChatClient GetOrCreateClient(string sessionKey)
		{
			string cacheKey = $"ChatClient_{sessionKey}";

			lock (_lock)
			{
				if (_cache.TryGetValue<IChatClient>(cacheKey, out var client))
				{
					// TryGetValue returns true only when client is not null
					return client!;
				}

				var newClient = new SemanticKernelClient(_serviceUrl, _model, _apiKey);
				var cacheOptions = new MemoryCacheEntryOptions
				{
					SlidingExpiration = TimeSpan.FromMinutes(30),
					Priority = CacheItemPriority.Normal
				};
				_cache.Set(cacheKey, newClient, cacheOptions);
				return newClient;
			}
		}
	}
}
