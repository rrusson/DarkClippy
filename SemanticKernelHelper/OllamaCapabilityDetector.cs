using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using SharedInterfaces;
using System.Linq;

namespace SemanticKernelHelper
{
	/// <summary>
	/// Detects model capabilities by querying Ollama's API.
	/// </summary>
	public class OllamaCapabilityDetector : IModelCapabilityDetector, IDisposable
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger? _logger;
		private readonly Dictionary<string, bool> _cache = new();
		private readonly SemaphoreSlim _cacheLock = new(1, 1);
		private bool _disposed;

		// Known models that support tools/function calling
		private static readonly HashSet<string> _knownToolSupportModels =
		[
			"llama3.1", "llama3.2", "llama3.3",
			"qwen2.5", "qwen2",
			"mistral-nemo", "mistral-small",
			"gemma2",
			"command-r", "command-r-plus"
		];

		/// <summary>
		/// Initializes a new instance of the OllamaCapabilityDetector class.
		/// </summary>
		/// <param name="serviceUrl">The base URL of the Ollama service.</param>
		/// <param name="logger">Optional logger for diagnostic information.</param>
		public OllamaCapabilityDetector(string serviceUrl, ILogger? logger = null)
		{
			ArgumentNullException.ThrowIfNull(serviceUrl);

			_httpClient = new HttpClient
			{
				BaseAddress = new Uri(serviceUrl)
			};
			_logger = logger;
		}

		/// <summary>
		/// Checks if the specified model supports tool/function calling.
		/// </summary>
		/// <param name="modelName">The name of the model to check.</param>
		/// <returns>True if the model supports tools/function calling, false otherwise.</returns>
		public async Task<bool> SupportsToolsAsync(string modelName)
		{
			if (string.IsNullOrWhiteSpace(modelName))
			{
				return false;
			}

			// Check cache first
			await _cacheLock.WaitAsync().ConfigureAwait(false);
			try
			{
				if (_cache.TryGetValue(modelName, out bool cachedResult))
				{
					return cachedResult;
				}
			}
			finally
			{
				_cacheLock.Release();
			}

			// Query Ollama API for model information
			bool supportsTools = await CheckModelCapabilityAsync(modelName).ConfigureAwait(false);

			// Cache the result
			await _cacheLock.WaitAsync().ConfigureAwait(false);
			try
			{
				_cache[modelName] = supportsTools;
			}
			finally
			{
				_cacheLock.Release();
			}

			return supportsTools;
		}

		/// <summary>
		/// Queries Ollama API to check if the model supports tools.
		/// </summary>
		/// <param name="modelName">The model name to check.</param>
		/// <returns>True if the model supports tools, false otherwise.</returns>
		private async Task<bool> CheckModelCapabilityAsync(string modelName)
		{
			try
			{
				// First, check against known models
				string normalizedName = modelName.ToLowerInvariant();

				var knownModel = _knownToolSupportModels.FirstOrDefault(knownModel => normalizedName.Contains(knownModel));
				if (knownModel != null)
				{
					_logger?.LogInformation("Model '{ModelName}' matches known tool-supporting model '{KnownModel}'", modelName, knownModel);
					return true;
				}

				// Try to get model info from Ollama API
				var request = new { name = modelName };
				var response = await _httpClient.PostAsJsonAsync("/api/show", request).ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					_logger?.LogWarning("Failed to get model info from Ollama for '{ModelName}': {StatusCode}", modelName, response.StatusCode);
					return false;
				}

				var modelInfo = await response.Content.ReadFromJsonAsync<OllamaShowResponse>().ConfigureAwait(false);

				if (modelInfo?.Details?.Families != null)
				{
					// Check if any of the model families are known to support tools
					foreach (var family in modelInfo.Details.Families)
					{
						string normalizedFamily = family.ToLowerInvariant();
						if (_knownToolSupportModels.Any(knownModel => normalizedFamily.Contains(knownModel)))
						{
							_logger?.LogInformation("Model '{ModelName}' family '{Family}' supports tools", modelName, family);
							return true;
						}
					}
				}

				// Check template for tool support indicators
				if (modelInfo?.Template != null)
				{
					string template = modelInfo.Template.ToLowerInvariant();
					if (template.Contains("tool") || template.Contains("function"))
					{
						_logger?.LogInformation("Model '{ModelName}' template indicates tool support", modelName);
						return true;
					}
				}

				_logger?.LogInformation("Model '{ModelName}' does not appear to support tools", modelName);
				return false;
			}
			catch (Exception ex)
			{
				_logger?.LogError(ex, "Error checking model capabilities for '{ModelName}'", modelName);
				return false;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
			{
				return;
			}

			if (disposing)
			{
				_httpClient?.Dispose();
				_cacheLock?.Dispose();
			}

			_disposed = true;
		}
	}
}
