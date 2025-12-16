using System.Net.Sockets;

namespace SharedInterfaces
{
    /// <summary>
    /// Factory for creating TcpClient instances.
    /// </summary>
    public interface ITcpClientFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="ITcpClient"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="ITcpClient"/>.</returns>
        ITcpClient Create();
    }
}