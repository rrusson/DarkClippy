namespace ClippyWeb.Util.UnitTests
{
	/// <summary>
	/// Unit tests for the <see cref="ClippyWeb.Util.TcpClientFactory"/> class.
	/// </summary>
	[TestClass]
	public class TcpClientFactoryTests
	{
		/// <summary>
		/// Tests that the Dispose method calls Dispose(bool) with true parameter.
		/// </summary>
		[TestMethod]
		public void CreateWorks()
		{
			// Act
			var result = new TcpClientFactory().Create();

			// Assert
			Assert.IsNotNull(result);
			Assert.IsInstanceOfType(result, typeof(TcpClientWrapper));
		}
	}
}
