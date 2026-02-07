using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using SharedInterfaces;

namespace SemanticKernelHelper
{
	public class ChatClientFactory : IChatClientFactory
	{
		private readonly string _serviceUrl;
		private readonly string _model;
		private readonly string _apiKey;
		private readonly IMemoryCache _cache;
		private readonly IMcpServerRegistry? _mcpServerRegistry;
		private readonly IModelCapabilityDetector? _capabilityDetector;
		private readonly ILogger? _logger;
		private readonly object _lock = new();

		public ChatClientFactory(
			string serviceUrl,
			string model,
			string apiKey,
			IMemoryCache cache,
			IMcpServerRegistry? mcpServerRegistry = null,
			IModelCapabilityDetector? capabilityDetector = null,
			ILogger? logger = null)
		{
			_serviceUrl = serviceUrl;
			_model = model;
			_apiKey = apiKey;
			_cache = cache;
			_mcpServerRegistry = mcpServerRegistry;
			_capabilityDetector = capabilityDetector;
			_logger = logger;
		}

		public async Task<IChatClient> GetOrCreateClientAsync(string sessionKey)
		{
			string cacheKey = $"ChatClient_{sessionKey}";

			lock (_lock)
			{
				if (_cache.TryGetValue<IChatClient>(cacheKey, out var client))
				{
					// TryGetValue returns true only when client is not null
					return client!;
				}
			}

			// Get enabled MCP servers
			IEnumerable<StdioMcpServer>? mcpServers = null;
			if (_mcpServerRegistry != null)
			{
				mcpServers = _mcpServerRegistry.GetEnabled().OfType<StdioMcpServer>();
			}

			var newClient = await SemanticKernelClient.CreateAsync(_serviceUrl, _model, _apiKey, mcpServers, _capabilityDetector, _logger).ConfigureAwait(false);

			lock (_lock)
			{
				var cacheOptions = new MemoryCacheEntryOptions
				{
					SlidingExpiration = TimeSpan.FromMinutes(30),
					Priority = CacheItemPriority.Normal
				};
				_cache.Set(cacheKey, newClient, cacheOptions);
			}

			return newClient;
		}
	}
}
