using SharedInterfaces;
using System.Net.Sockets;

namespace ClippyWeb.Util
{
	/// <inheritdoc/>
	public class TcpClientFactory : ITcpClientFactory
	{
		/// <inheritdoc/>
		public ITcpClient Create()
		{
			return new TcpClientWrapper();
		}
	}
}
