using SharedInterfaces;
using Microsoft.AspNetCore.Mvc;
using MarkdownSharp;
using Microsoft.Extensions.Caching.Memory;

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

		public ChatController(IChatClient chatClient, IMemoryCache cache)
		{
			_chatClient = chatClient;
			_cache = cache;
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] string question)
		{
			if (_cache.Get<bool>(nameof(RequestInProgressKey)))
			{
				return StatusCode(429, "I'm kind of busy chatting with someone else. Try again later.");
			}

			_cache.Set(nameof(RequestInProgressKey), true);

			try
			{
				var response = await _chatClient.GetChatResponseAsync(question);
				var htmlResponse = _markdownConverter.Transform(response);
				return Ok(htmlResponse);
			}
			finally
			{
				_cache.Set(nameof(RequestInProgressKey), false);
			}
		}
	}
}
