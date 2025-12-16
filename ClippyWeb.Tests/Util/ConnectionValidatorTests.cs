using System.Net;
using System.Net.Sockets;

using ClippyWeb.Util;

using Microsoft.Extensions.Configuration;

using SharedInterfaces;

namespace ClippyWeb.Tests.Util
{
	/// <summary>
	/// Tests for <see cref="ConnectionValidator"/>
	/// </summary>
	[TestClass]
	public class ConnectionValidatorTests
	{
		private Mock<IConfiguration> _mockConfiguration = null!;
		private Mock<IPingService> _mockPingService = null!;
		private Mock<ITcpClient> _mockTcpClient = null!;
		private Mock<ITcpClientFactory> _mockTcpClientFactory = null!;
		private ConnectionValidator _sut = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_mockConfiguration = new Mock<IConfiguration>();
			_mockPingService = new Mock<IPingService>();
			_mockTcpClientFactory = new Mock<ITcpClientFactory>();
			_mockPingService.Setup(x => x.PingAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(true);
			_mockTcpClient = new Mock<ITcpClient>();
			_mockTcpClient.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
			_mockTcpClient.Setup(t => t.Connected).Returns(true);
			_mockTcpClientFactory.Setup(x => x.Create()).Returns(_mockTcpClient.Object);

			_sut = new ConnectionValidator(_mockPingService.Object, _mockTcpClientFactory.Object);
		}

		[DataTestMethod]
		[DataRow(null, DisplayName = "Null ServiceUrl")]
		[DataRow("", DisplayName = "Empty ServiceUrl")]
		public async Task IfServiceUrlIsNullOrEmptyThenLogsWarningAndReturns(string? serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act
			await _sut.ValidateConnectionAsync(_mockConfiguration.Object);

			// Assert
			_mockConfiguration.Verify(x => x["ServiceUrl"], Times.Once);
			_mockConfiguration.VerifyNoOtherCalls();
		}

		[DataTestMethod]
		[DataRow("http://localhost:11434", DisplayName = "Localhost with port")]
		[DataRow("http://127.0.0.1:8080", DisplayName = "127.0.0.1 with port")]
		[DataRow("http://localhost", DisplayName = "Localhost without port")]
		[DataRow("https://localhost:443", DisplayName = "HTTPS URL")]
		[DataRow("http://localhost:11434/api/chat", DisplayName = "URL with path")]
		[DataRow("http://localhost:11434?param=value", DisplayName = "URL with query string")]
		public async Task ValidateConnectionAsync_LocalhostTest(string serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act
			await _sut.ValidateConnectionAsync(_mockConfiguration.Object);

			// Assert
			_mockConfiguration.Verify(x => x["ServiceUrl"], Times.Once);
			_mockTcpClient.Verify(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
		}

		[DataTestMethod]
		[DataRow("http://example.com", DisplayName = "Remote host, no port")]
		[DataRow("http://example.com:8080", DisplayName = "Remote host with port")]
		[DataRow("http://testhost:5000", DisplayName = "Test host with port")]
		public async Task ValidateConnectionAsync_RemoteTest(string serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act
			await _sut.ValidateConnectionAsync(_mockConfiguration.Object);

			// Assert
			_mockConfiguration.Verify(x => x["ServiceUrl"], Times.Once);
			_mockPingService.Verify(t => t.PingAsync(It.IsAny<string>(), It.IsAny<int>()), Times.AtLeastOnce);
		}

		[DataTestMethod]
		[DataRow("not-a-valid-uri", DisplayName = "Invalid URI format")]
		[DataRow("   ", DisplayName = "Whitespace only")]
		public async Task IfServiceUrlIsInvalidUriThenThrowsException(string serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act & Assert
			await Assert.ThrowsExceptionAsync<UriFormatException>(() => _sut.ValidateConnectionAsync(_mockConfiguration.Object));
		}
	}
}