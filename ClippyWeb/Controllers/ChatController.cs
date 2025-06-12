using MarkdownSharp;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using SharedInterfaces;

using System.Net.Sockets;

namespace ClippyWeb.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ChatController : ControllerBase
	{
		private const object RequestInProgressKey = null;
		private readonly IChatClient _chatClient;
		private readonly Markdown _markdownConverter = new();
		private readonly IMemoryCache _cache;
		private readonly ILogger<ChatController> _logger;
		private readonly IConfiguration _configuration;

		public ChatController(IChatClient chatClient, IMemoryCache cache, ILogger<ChatController> logger, IConfiguration configuration)
		{
			_chatClient = chatClient;
			_cache = cache;
			_logger = logger;
			_configuration = configuration;
			_logger.LogInformation("ChatController initialized");
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] string question)
		{
			if (_cache.Get<bool>(nameof(RequestInProgressKey)))
			{
				_logger.LogWarning("[Request rejected due to existing request in progress]");
				return StatusCode(429, "I'm kind of busy chatting with someone else. Try again later.");
			}

			_cache.Set(nameof(RequestInProgressKey), true);
			_logger.LogInformation("Request lock acquired");

			try
			{
				var response = await _chatClient.GetChatResponseAsync(question);
				
				if (response == null)
				{
					_logger.LogError("Chat client returned null response");
					return StatusCode(500, "Failed to get a response from the chat service.");
				}

				string htmlResponse = _markdownConverter.Transform(response);
				return Ok(htmlResponse);
			}
			catch (SocketException ex)
			{
				string serviceUrl = _configuration["ServiceUrl"] ?? "unknown";
				_logger.LogError(ex, "Socket connection error to LLM service at {ServiceUrl}. Please check if Ollama is running.", serviceUrl);
				return StatusCode(503, "Cannot connect to the AI service. Please ensure Ollama is running on the server.");
			}
			catch (HttpRequestException ex) when (ex.Message.Contains("No connection could be made"))
			{
				string serviceUrl = _configuration["ServiceUrl"] ?? "unknown";
				_logger.LogError(ex, "Connection refused to LLM service at {ServiceUrl}. Please check if Ollama is running.", serviceUrl);
				return StatusCode(503, "Cannot connect to the AI service. Please ensure Ollama is running on the server.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing chat request for question: {Question}", question);
				
				// Provide more specific error message based on exception type
				string errorMessage = ex.Message.Contains("actively refused") || ex.Message.Contains("No connection") 
					? "Cannot connect to the AI service. Please ensure Ollama is running on the server."
					: "An error occurred while processing your request.";
					
				return StatusCode(500, errorMessage);
			}
			finally
			{
				_cache.Set(nameof(RequestInProgressKey), false);
				_logger.LogInformation("Request lock released");
			}
		}
	}
}
