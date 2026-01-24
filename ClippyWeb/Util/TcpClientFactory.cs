using SharedInterfaces;

namespace ClippyWeb.Util
{
	/// <inheritdoc/>
	public class TcpClientFactory : ITcpClientFactory
	{
		/// <inheritdoc/>
		public ITcpClient Create() => new TcpClientWrapper();
	}
}
