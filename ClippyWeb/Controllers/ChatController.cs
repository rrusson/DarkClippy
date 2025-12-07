using MarkdownSharp;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using SharedInterfaces;

using System.Net.Sockets;
using Serilog;

namespace ClippyWeb.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ChatController : ControllerBase
	{
		private const string RequestInProgressKey = nameof(RequestInProgressKey);
		private readonly IChatClient _chatClient;
		private readonly Markdown _markdownConverter = new();
		private readonly IMemoryCache _cache;
		private readonly IConfiguration _configuration;

		public ChatController(IChatClient chatClient, IMemoryCache cache, IConfiguration configuration)
		{
			_chatClient = chatClient;
			_cache = cache;
			_configuration = configuration;
			Log.Information("ChatController initialized");
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] string question)
		{
			if (_cache.Get<bool>(RequestInProgressKey))
			{
				Log.Warning("[Request rejected due to existing request in progress]");
				return StatusCode(StatusCodes.Status429TooManyRequests, "I'm kind of busy chatting with someone else. Try again later.");
			}

			_cache.Set(RequestInProgressKey, true);
			Log.Information("Request lock acquired");

			try
			{
				var response = await _chatClient.GetChatResponseAsync(question);
				
				if (response == null)
				{
					Log.Error("Chat client returned null response");
					return StatusCode(StatusCodes.Status500InternalServerError, "Failed to get a response from the chat service.");
				}

				string htmlResponse = _markdownConverter.Transform(response);
				return Ok(htmlResponse);
			}
			catch (SocketException ex)
			{
				string serviceUrl = _configuration["ServiceUrl"] ?? "unknown";
				Log.Error(ex, "Socket connection error to LLM service at {ServiceUrl}. Please check if Ollama is running.", serviceUrl);
				return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cannot connect to the AI service. Please ensure Ollama is running on the server.");
			}
			catch (HttpRequestException ex) when (ex.Message.Contains("No connection could be made"))
			{
				string serviceUrl = _configuration["ServiceUrl"] ?? "unknown";
				Log.Error(ex, "Connection refused to LLM service at {ServiceUrl}. Please check if Ollama is running.", serviceUrl);
				return StatusCode(StatusCodes.Status503ServiceUnavailable, "Cannot connect to the AI service. Please ensure Ollama is running on the server.");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error processing chat request for question: {Question}", question);
				
				// Provide more specific error message based on exception type
				string errorMessage = ex.Message.Contains("actively refused") || ex.Message.Contains("No connection") 
					? "Cannot connect to the AI service. Please ensure Ollama is running on the server."
					: "An error occurred while processing your request.";
					
				return StatusCode(StatusCodes.Status500InternalServerError, errorMessage);
			}
			finally
			{
				_cache.Set(RequestInProgressKey, false);
				Log.Information("Request lock released");
			}
		}
	}
}
