using SharedInterfaces;
using Microsoft.AspNetCore.Mvc;
using MarkdownSharp;

namespace ClippyWeb.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ChatController : ControllerBase
	{
		private readonly IChatClient _chatClient;
		private readonly Markdown _markdownConverter;

		public ChatController(IChatClient chatClient)
		{
			_chatClient = chatClient;
			_markdownConverter = new Markdown();
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] string question)
		{
			var response = await _chatClient.GetChatResponseAsync(question);
			var htmlResponse = _markdownConverter.Transform(response);

			return Ok(htmlResponse);
		}
	}
}
