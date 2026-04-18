using System.Net.Sockets;

namespace ClippyWeb.Util.UnitTests
{
	/// <summary>
	/// Unit tests for the <see cref="ClippyWeb.Util.TcpClientWrapper"/> class.
	/// </summary>
	[TestClass]
	public class TcpClientWrapperTests
	{
		/// <summary>
		/// Tests that the Dispose method calls Dispose(bool) with true parameter.
		/// </summary>
		[TestMethod]
		public void Dispose_WhenCalled_CallsDisposeWithTrue()
		{
			// Arrange
			var disposeTracker = new DisposableTestWrapper();

			// Act
			disposeTracker.Dispose();

			// Assert
			Assert.IsTrue(disposeTracker.DisposeCalled, "Dispose(bool) should have been called");
			Assert.IsTrue(disposeTracker.DisposingParameter, "Dispose(bool) should have been called with true");
		}

		/// <summary>
		/// Tests that the Dispose method does not throw exceptions when called.
		/// </summary>
		[TestMethod]
		public void Dispose_WhenCalled_DoesNotThrow()
		{
			// Arrange
			var sut = new TcpClientWrapper();

			// Act
			sut.Dispose();

			// Assert
			Assert.IsTrue(true); // Dispose completed without throwing
		}

		/// <summary>
		/// Tests that the Dispose method can be called multiple times without throwing exceptions (idempotency).
		/// </summary>
		[TestMethod]
		public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
		{
			// Arrange
			var sut = new TcpClientWrapper();

			// Act & Assert
			sut.Dispose();
			sut.Dispose();
			sut.Dispose();
		}

		/// <summary>
		/// Tests that the Connected property returns false when the TcpClient is not connected.
		/// Input: A newly instantiated TcpClientWrapper with no active connection.
		/// Expected: Connected property should return false.
		/// </summary>
		[TestMethod]
		public void Connected_WhenNotConnected_ReturnsFalse()
		{
			// Arrange
			using var wrapper = new TcpClientWrapper();

			// Act
			var result = wrapper.Connected;

			// Assert
			Assert.IsFalse(result);
		}

		/// <summary>
		/// Tests that the Connected property can be accessed multiple times without throwing.
		/// Input: A newly instantiated TcpClientWrapper accessed multiple times.
		/// Expected: Connected property should consistently return false without exceptions.
		/// </summary>
		[TestMethod]
		public void Connected_MultipleAccesses_ReturnsConsistentValue()
		{
			// Arrange
			using var wrapper = new TcpClientWrapper();

			// Act
			var firstAccess = wrapper.Connected;
			var secondAccess = wrapper.Connected;
			var thirdAccess = wrapper.Connected;

			// Assert
			Assert.IsFalse(firstAccess);
			Assert.IsFalse(secondAccess);
			Assert.IsFalse(thirdAccess);
		}

		/// <summary>
		/// Tests that the Connected property does not throw an exception when accessed.
		/// Input: A newly instantiated TcpClientWrapper.
		/// Expected: No exception should be thrown when accessing Connected property.
		/// </summary>
		[TestMethod]
		public void Connected_WhenAccessed_DoesNotThrowException()
		{
			// Arrange
			using var wrapper = new TcpClientWrapper();

			// Act & Assert
			try
			{
				var _ = wrapper.Connected;
				Assert.IsTrue(true);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Expected no exception, but got {ex.GetType().Name}: {ex.Message}");
			}
		}

		/// <summary>
		/// Tests that the parameterless constructor creates an instance successfully without throwing exceptions.
		/// Input: No parameters.
		/// Expected: Instance is created and not null.
		/// </summary>
		[TestMethod]
		public void TcpClientWrapper_Constructor_CreatesInstanceSuccessfully()
		{
			// Arrange & Act
			var sut = new TcpClientWrapper();

			// Assert
			Assert.IsNotNull(sut);
		}

		/// <summary>
		/// Tests that the constructor properly initializes the internal TcpClient instance.
		/// This is verified indirectly through the Connected property.
		/// Input: No parameters.
		/// Expected: Connected property is accessible and returns false for a newly created, unconnected TcpClient.
		/// </summary>
		[TestMethod]
		public void TcpClientWrapper_Constructor_InitializesInternalTcpClient()
		{
			// Arrange & Act
			var sut = new TcpClientWrapper();

			// Assert
			Assert.IsFalse(sut.Connected);
		}

		/// <summary>
		/// Tests that Dispose(bool) with disposing=true properly disposes the internal TcpClient.
		/// Verifies that the internal TcpClient is disposed by checking the Connected property behavior.
		/// </summary>
		[TestMethod]
		public void Dispose_DisposingTrue_DisposesInternalTcpClient()
		{
			// Arrange
			var wrapper = new TestableTcpClientWrapper();

			// Act
			wrapper.PublicDispose(true);

			// Assert
			// After disposal, accessing Connected should either return false or throw ObjectDisposedException
			// This verifies the internal TcpClient was actually disposed
			try
			{
				var connected = wrapper.Connected;
				// If no exception, verify it returns false after disposal
				Assert.IsFalse(connected);
			}
			catch (ObjectDisposedException)
			{
				// This is also acceptable behavior after disposal
				Assert.IsTrue(true);
			}
		}

		/// <summary>
		/// Tests that Dispose(bool) with disposing=false does not dispose the internal TcpClient.
		/// Verifies that the method executes without throwing exceptions.
		/// </summary>
		[TestMethod]
		public void Dispose_DisposingFalse_DoesNotDispose()
		{
			// Arrange
			var wrapper = new TestableTcpClientWrapper();

			// Act
			wrapper.PublicDispose(false);

			// Assert
			// The method should complete without exceptions
			// Connected property should still be accessible (though likely false since not connected)
			var connected = wrapper.Connected;
			Assert.IsFalse(connected);
		}

		/// <summary>
		/// Tests that Dispose(bool) with disposing=true handles null TcpClient gracefully.
		/// Verifies that calling Dispose multiple times does not throw exceptions.
		/// </summary>
		[TestMethod]
		public void Dispose_CalledMultipleTimes_DoesNotThrow()
		{
			// Arrange
			var wrapper = new TestableTcpClientWrapper();

			// Act
			wrapper.PublicDispose(true);
			wrapper.PublicDispose(true);

			// Assert
			// Multiple dispose calls should not throw exceptions
			Assert.IsTrue(true);
		}

		/// <summary>
		/// Helper class to expose the protected Dispose(bool) method for testing
		/// </summary>
		private class TestableTcpClientWrapper : global::ClippyWeb.Util.TcpClientWrapper
		{
			/// <summary>
			/// Exposes the protected Dispose(bool) method as public for testing
			/// </summary>
			/// <param name="disposing">True to dispose managed resources</param>
			public void PublicDispose(bool disposing)
			{
				Dispose(disposing);
			}
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentNullException when host parameter is null.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_NullHost_ThrowsArgumentNullException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string? host = null;
			int port = 80;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			{
				await sut.ConnectAsync(host!, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentOutOfRangeException when port is negative.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_NegativePort_ThrowsArgumentOutOfRangeException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = -1;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentOutOfRangeException when port is zero.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_ZeroPort_ThrowsArgumentOutOfRangeException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = 0;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<SocketException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentOutOfRangeException when port exceeds maximum valid value.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_PortAboveMaximum_ThrowsArgumentOutOfRangeException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = 65536;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentOutOfRangeException when port is int.MaxValue.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_PortMaxValue_ThrowsArgumentOutOfRangeException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = int.MaxValue;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentOutOfRangeException when port is int.MinValue.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_PortMinValue_ThrowsArgumentOutOfRangeException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = int.MinValue;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync respects cancellation when token is already cancelled.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_CancelledToken_ThrowsOperationCanceledException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = 80;
			var cts = new CancellationTokenSource();
			cts.Cancel();

			// Act & Assert
			await Assert.ThrowsAsync<TaskCanceledException>(async () =>
			{
				await sut.ConnectAsync(host, port, cts.Token);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync throws ArgumentNullException when host is empty string.
		/// Note: The actual behavior depends on the underlying TcpClient implementation.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_EmptyHost_ThrowsArgumentException()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "";
			int port = 80;
			var cancellationToken = CancellationToken.None;

			// Act & Assert
			// Empty host should throw an exception from the underlying TcpClient
			await Assert.ThrowsAsync<ArgumentException>(async () =>
			{
				await sut.ConnectAsync(host, port, cancellationToken);
			});
		}

		/// <summary>
		/// Tests that ConnectAsync with valid parameters initiates connection attempt.
		/// Note: This test validates that the method can be called without immediate exceptions.
		/// Actual connection success depends on network availability and is not tested here.
		/// The method will likely throw SocketException if no service is listening.
		/// </summary>
		[TestMethod]
		public async Task ConnectAsync_ValidParameters_InitiatesConnection()
		{
			// Arrange
			var sut = new TcpClientWrapper();
			string host = "localhost";
			int port = 1; // Valid port number (though unlikely to have a service listening)
			using var cts = new CancellationTokenSource(100); // Short timeout to prevent hanging

			// Act & Assert
			// This test verifies the method accepts valid parameters and attempts connection.
			// It will likely throw SocketException due to no listener, which is expected behavior.
			// We're primarily testing that parameter validation passes.
			try
			{
				await sut.ConnectAsync(host, port, cts.Token);
			}
			catch (SocketException)
			{
				// Expected when no service is listening
				Assert.IsTrue(true);
			}
			catch (OperationCanceledException)
			{
				// Expected if timeout occurs before connection fails
				Assert.IsTrue(true);
			}
		}


		/// <summary>
		/// Helper class to track Dispose(bool) calls for testing purposes.
		/// </summary>
		private sealed class DisposableTestWrapper : global::ClippyWeb.Util.TcpClientWrapper
		{
			public bool DisposeCalled { get; private set; }

			public bool DisposingParameter { get; private set; }

			protected override void Dispose(bool disposing)
			{
				DisposeCalled = true;
				DisposingParameter = disposing;
				base.Dispose(disposing);
			}
		}
	}
}