using System.Net.Sockets;

namespace SharedInterfaces
{
    /// <summary>
    /// Abstraction for TcpClient to enable testability.
    /// </summary>
    public interface ITcpClient : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether the underlying Socket is connected to a remote host.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Connects the client to a remote TCP host using the specified host name, port number, and cancellation token as an asynchronous operation.
        /// </summary>
        /// <param name="host">The DNS name of the remote host to which you intend to connect.</param>
        /// <param name="port">The port number of the remote host to which you intend to connect.</param>
        /// <param name="cancellationToken">A cancellation token used to propagate notification that this operation should be canceled.</param>
        /// <returns>A task that represents the asynchronous connection operation.</returns>
        ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken);
    }
}
