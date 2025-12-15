using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

using System.Diagnostics;

using ClippyWeb.Pages;

namespace ClippyWeb.Tests.Pages
{
	/// <summary>
	/// Tests for <see cref="ErrorModel"/>
	/// </summary>
	[TestClass]
	public class ErrorModelTests
	{
		private Mock<IConfiguration> _mockConfiguration = null!;
		private ErrorModel _sut = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_mockConfiguration = new Mock<IConfiguration>();
			_sut = new ErrorModel(_mockConfiguration.Object);
			SetupHttpContext();
		}

		[TestMethod]
		public void IfActivityCurrentIdExistsThenRequestIdIsSetToActivityId()
		{
			// Arrange
			var activity = new Activity("TestActivity");
			activity.Start();

			// Act
			_sut.OnGet();

			// Assert
			Assert.IsNotNull(_sut.RequestId);
			Assert.AreEqual(activity.Id, _sut.RequestId);

			activity.Stop();
			activity.Dispose();
		}

		[TestMethod]
		public void IfActivityCurrentIsNullThenRequestIdIsSetToTraceIdentifier()
		{
			// Arrange
			const string traceIdentifier = "test-trace-id";
			_sut.HttpContext.TraceIdentifier = traceIdentifier;

			// Act
			_sut.OnGet();

			// Assert
			Assert.AreEqual(traceIdentifier, _sut.RequestId);
		}

		[TestMethod]
		public void IfRequestIdIsSetThenShowRequestIdReturnsTrue()
		{
			// Arrange
			_sut.HttpContext.TraceIdentifier = "test-id";

			// Act
			_sut.OnGet();

			// Assert
			Assert.IsTrue(_sut.ShowRequestId);
		}

		[DataTestMethod]
		[DataRow(null, DisplayName = "Null RequestId")]
		[DataRow("", DisplayName = "Empty RequestId")]
		public void IfRequestIdIsNullOrEmptyThenShowRequestIdReturnsFalse(string? requestId)
		{
			// Arrange
			_sut.RequestId = requestId;

			// Act
			var result = _sut.ShowRequestId;

			// Assert
			Assert.IsFalse(result);
		}

		[DataTestMethod]
		[DataRow("http://localhost:11434", "http://localhost:11434", DisplayName = "ServiceUrl configured")]
		[DataRow(null, "Not configured", DisplayName = "ServiceUrl not configured")]
		public void IfServiceUrlConfiguredThenViewDataContainsExpectedValue(string? configuredUrl, string expectedValue)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(configuredUrl);

			// Act
			_sut.OnGet();

			// Assert
			Assert.AreEqual(expectedValue, _sut.ViewData["ServiceUrl"]);
		}

		[DataTestMethod]
		[DataRow("llama2", "llama2", DisplayName = "Model configured")]
		[DataRow(null, "Not configured", DisplayName = "Model not configured")]
		public void IfModelConfiguredThenViewDataContainsExpectedValue(string? configuredModel, string expectedValue)
		{
			// Arrange
			_mockConfiguration.Setup(x => x["Model"]).Returns(configuredModel);

			// Act
			_sut.OnGet();

			// Assert
			Assert.AreEqual(expectedValue, _sut.ViewData["ModelName"]);
		}

		[TestMethod]
		public void IfOnGetCalledThenBothViewDataPropertiesAreSet()
		{
			// Arrange
			const string serviceUrl = "http://localhost:11434";
			const string modelName = "llama2";
			_mockConfiguration.Setup(x => x["ServiceUrl"]).Returns(serviceUrl);
			_mockConfiguration.Setup(x => x["Model"]).Returns(modelName);

			// Act
			_sut.OnGet();

			// Assert
			Assert.AreEqual(serviceUrl, _sut.ViewData["ServiceUrl"]);
			Assert.AreEqual(modelName, _sut.ViewData["ModelName"]);
		}

		private void SetupHttpContext()
		{
			var httpContext = new DefaultHttpContext
			{
				TraceIdentifier = "default-trace-id"
			};

			_sut.PageContext = new PageContext
			{
				HttpContext = httpContext,
				ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
					new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
					new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
			};
		}
	}
}