using System.Net.Sockets;

using SharedInterfaces;

namespace ClippyWeb.Util
{
    /// <summary>
    /// Wrapper for TcpClient to enable testability
    /// </summary>
    public class TcpClientWrapper : ITcpClient
    {
        private readonly TcpClient _tcpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpClientWrapper"/> class
        /// </summary>
        public TcpClientWrapper()
        {
            _tcpClient = new TcpClient();
        }

        /// <inheritdoc/>
        public bool Connected => _tcpClient.Connected;

        /// <inheritdoc/>
        public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            return _tcpClient.ConnectAsync(host, port, cancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
		}

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tcpClient?.Dispose();
            }
		}
	}
}
