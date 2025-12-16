using System.Net.NetworkInformation;

namespace ClippyWeb.Util
{
	/// <inheritdoc/>
	public class PingService : IPingService
	{
		/// <inheritdoc/>
		public async Task<bool> PingAsync(string host, int timeoutMs)
		{
			using var ping = new Ping();
			try
			{
				var reply = await ping.SendPingAsync(host, timeoutMs).ConfigureAwait(false);
				return reply.Status == IPStatus.Success;
			}
			catch
			{
				return false;
			}
		}
	}
}
