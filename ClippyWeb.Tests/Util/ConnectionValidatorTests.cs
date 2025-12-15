using Microsoft.Extensions.Configuration;

using ClippyWeb.Util;

namespace ClippyWeb.Tests.Util
{
	/// <summary>
	/// Tests for <see cref="ConnectionValidator"/>
	/// </summary>
	[TestClass]
	public class ConnectionValidatorTests
	{
		private Mock<IConfiguration> _mockConfiguration = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_mockConfiguration = new Mock<IConfiguration>();
		}

		[DataTestMethod]
		[DataRow(null, DisplayName = "Null ServiceUrl")]
		[DataRow("", DisplayName = "Empty ServiceUrl")]
		public void IfServiceUrlIsNullOrEmptyThenLogsWarningAndReturns(string? serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act
			ConnectionValidator.ValidateConnection(_mockConfiguration.Object);

			// Assert
			_mockConfiguration.Verify(x => x["ServiceUrl"], Times.Once);
			_mockConfiguration.VerifyNoOtherCalls();

		}

		[DataTestMethod]
		[DataRow("http://localhost:11434", DisplayName = "Localhost with port")]
		[DataRow("http://127.0.0.1:8080", DisplayName = "127.0.0.1 with port")]
		[DataRow("http://example.com:8080", DisplayName = "Remote host with port")]
		[DataRow("http://localhost", DisplayName = "Localhost without port")]
		[DataRow("https://localhost:443", DisplayName = "HTTPS URL")]
		[DataRow("http://testhost:5000", DisplayName = "Test host with port")]
		[DataRow("http://localhost:11434/api/chat", DisplayName = "URL with path")]
		[DataRow("http://localhost:11434?param=value", DisplayName = "URL with query string")]
		public void IfServiceUrlIsValidThenConfigurationIsAccessed(string serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act
			ConnectionValidator.ValidateConnection(_mockConfiguration.Object);

			// Assert
			_mockConfiguration.Verify(x => x["ServiceUrl"], Times.Once);
		}

		[DataTestMethod]
		[DataRow("not-a-valid-uri", DisplayName = "Invalid URI format")]
		[DataRow("   ", DisplayName = "Whitespace only")]
		public void IfServiceUrlIsInvalidUriThenThrowsException(string serviceUrl)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);

			// Act & Assert
			Assert.ThrowsException<UriFormatException>(() => ConnectionValidator.ValidateConnection(_mockConfiguration.Object));
		}
	}
}