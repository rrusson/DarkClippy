using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Sockets;

using ClippyWeb.Controllers;
using SharedInterfaces;

namespace ClippyWeb.Tests.Controllers
{
	/// <summary>
	/// Tests for <see cref="ChatController"/>
	/// </summary>
	[TestClass]
	public class ChatControllerTests
	{
		private Mock<IChatClient> _mockChatClient = null!;
		private IMemoryCache _memoryCache = null!;
		private Mock<IConfiguration> _mockConfiguration = null!;
		private ChatController _sut = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_mockChatClient = new Mock<IChatClient>();
			_memoryCache = new MemoryCache(new MemoryCacheOptions());
			_mockConfiguration = new Mock<IConfiguration>();

			_sut = new ChatController(_mockChatClient.Object, _memoryCache, _mockConfiguration.Object);
			SetupHttpContext();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_memoryCache?.Dispose();
		}

		[TestMethod]
		public async Task IfQuestionIsProvidedThenReturnsOkWithResponse()
		{
			// Arrange
			const string question = "What is the meaning of life?";
			const string expectedResponse = "42";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync(expectedResponse);

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.IsNotNull(okResult);
			Assert.IsNotNull(okResult.Value);
			Assert.IsTrue(okResult.Value.ToString()!.Contains(expectedResponse));
		}

		[TestMethod]
		public async Task IfRequestInProgressThenReturns429()
		{
			// Arrange
			const string question = "Test question";
			_memoryCache.Set("RequestInProgressKey", true);

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
		}

		[TestMethod]
		public async Task IfChatClientReturnsNullThenReturns500()
		{
			// Arrange
			const string question = "Test question";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync((string?)null);

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
			Assert.IsTrue(objectResult.Value!.ToString()!.Contains("Failed to get a response from the chat service"));
		}

		[TestMethod]
		public async Task IfSocketExceptionOccursThenReturns503()
		{
			// Arrange
			const string question = "Test question";
			const string serviceUrl = "http://localhost:11434";
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new SocketException());

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
			Assert.IsTrue(objectResult.Value!.ToString()!.Contains("Cannot connect to the AI service"));
		}

		[TestMethod]
		public async Task IfHttpRequestExceptionWithNoConnectionThenReturns503()
		{
			// Arrange
			const string question = "Test question";
			const string serviceUrl = "http://localhost:11434";
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new HttpRequestException("No connection could be made"));

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
			Assert.IsTrue(objectResult.Value!.ToString()!.Contains("Cannot connect to the AI service"));
		}

		[TestMethod]
		public async Task IfHttpRequestExceptionWithDifferentMessageThenReturns500()
		{
			// Arrange
			const string question = "Test question";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new HttpRequestException("Different error"));

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
		}

		[TestMethod]
		public async Task IfGenericExceptionWithActivelyRefusedThenReturnsConnectionError()
		{
			// Arrange
			const string question = "Test question";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new Exception("Connection actively refused"));

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
			Assert.IsTrue(objectResult.Value!.ToString()!.Contains("Cannot connect to the AI service"));
		}

		[TestMethod]
		public async Task IfGenericExceptionOccursThenReturns500()
		{
			// Arrange
			const string question = "Test question";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new Exception("Generic error"));

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
			Assert.IsTrue(objectResult.Value!.ToString()!.Contains("An error occurred while processing your request"));
		}

		[TestMethod]
		public async Task IfRequestCompleteThenLockIsReleased()
		{
			// Arrange
			const string question = "Test question";
			const string expectedResponse = "Response";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync(expectedResponse);

			// Act
			await _sut.Post(question);

			// Assert
			var lockStatus = _memoryCache.Get<bool>("RequestInProgressKey");
			Assert.IsFalse(lockStatus);
		}

		[TestMethod]
		public async Task IfExceptionOccursThenLockIsReleased()
		{
			// Arrange
			const string question = "Test question";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new Exception("Test exception"));

			// Act
			await _sut.Post(question);

			// Assert
			var lockStatus = _memoryCache.Get<bool>("RequestInProgressKey");
			Assert.IsFalse(lockStatus);
		}

		[TestMethod]
		public async Task IfMarkdownResponseThenConvertsToHtml()
		{
			// Arrange
			const string question = "Test question";
			const string markdownResponse = "**Bold text**";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync(markdownResponse);

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<OkObjectResult>(result);
			var okResult = result as OkObjectResult;
			Assert.IsNotNull(okResult);
			var htmlResponse = okResult.Value!.ToString();
			Assert.IsTrue(htmlResponse!.Contains("<strong>"));
		}

		[TestMethod]
		public async Task IfServiceUrlNotConfiguredThenUsesUnknownInLog()
		{
			// Arrange
			const string question = "Test question";
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns((string?)null);
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ThrowsAsync(new SocketException());

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(result);
			var objectResult = result as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
		}

		[TestMethod]
		public async Task IfMultipleRequestsThenSecondRequestIsRejected()
		{
			// Arrange
			const string question = "Test question";
			const string expectedResponse = "Response";
			var tcs = new TaskCompletionSource<string?>();

			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).Returns(tcs.Task);

			// Act - Start first request
			var firstRequestTask = _sut.Post(question);

			// Give first request time to acquire lock
			await Task.Delay(10);

			// Start second request
			var secondResult = await _sut.Post(question);

			// Complete first request
			tcs.SetResult(expectedResponse);
			await firstRequestTask;

			// Assert
			Assert.IsInstanceOfType<ObjectResult>(secondResult);
			var objectResult = secondResult as ObjectResult;
			Assert.IsNotNull(objectResult);
			Assert.AreEqual(StatusCodes.Status429TooManyRequests, objectResult.StatusCode);
		}

		[TestMethod]
		public async Task IfRemoteIpAddressIsNullThenUsesUnknownIp()
		{
			// Arrange
			const string question = "Test question";
			const string expectedResponse = "Response";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync(expectedResponse);

			var httpContext = new DefaultHttpContext();
			httpContext.Connection.RemoteIpAddress = null;

			_sut.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};

			// Act
			var result = await _sut.Post(question);

			// Assert
			Assert.IsInstanceOfType<OkObjectResult>(result);
		}

		[TestMethod]
		public async Task IfChatClientIsCalledThenQuestionIsPassedCorrectly()
		{
			// Arrange
			const string question = "What is AI?";
			const string expectedResponse = "AI is Artificial Intelligence";
			_mockChatClient.Setup(x => x.GetChatResponseAsync(question)).ReturnsAsync(expectedResponse);

			// Act
			await _sut.Post(question);

			// Assert
			_mockChatClient.Verify(x => x.GetChatResponseAsync(question), Times.Once);
		}


		private void SetupHttpContext()
		{
			var httpContext = new DefaultHttpContext
			{
				Connection =
				{
					RemoteIpAddress = IPAddress.Parse("127.0.0.1")
				}
			};

			_sut.ControllerContext = new ControllerContext
			{
				HttpContext = httpContext
			};
		}
	}
}
