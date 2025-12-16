using System.Net.Sockets;

using Microsoft.Extensions.Configuration;

using Serilog;
using SharedInterfaces; // Add this using directive to resolve ITcpClient and ITcpClientFactory

namespace ClippyWeb.Util
{
	/// <inheritdoc/>
	public class ConnectionValidator : SharedInterfaces.IConnectionValidator
	{
		private readonly IPingService _pingService;
		private readonly ITcpClientFactory _tcpClientFactory;

		public ConnectionValidator(IPingService pingService, ITcpClientFactory tcpClientFactory)
		{
			_pingService = pingService;
			_tcpClientFactory = tcpClientFactory;
		}

		/// <inheritdoc/>
		public async Task ValidateConnectionAsync(IConfiguration configuration)
		{
			var serviceUrl = configuration["ServiceUrl"];
			if (string.IsNullOrEmpty(serviceUrl))
			{
				Log.Warning("ServiceUrl not configured in appsettings.json");
				return;
			}

			var uri = new Uri(serviceUrl);
			var host = uri.Host;
			var port = uri.Port;

			Log.Information("Validating connection to {host}:{port}", host, port);

			if (host == "localhost" || host == "127.0.0.1")
			{
				await TryLocalConnectionAsync(host, port).ConfigureAwait(false);
			}
			else
			{
				await TryRemoteConnectionAsync(host).ConfigureAwait(false);
			}
		}

		private async Task TryLocalConnectionAsync(string host, int port)
		{
			try
			{
				using SharedInterfaces.ITcpClient client = _tcpClientFactory.Create();
				CancellationToken cancellationToken = new CancellationTokenSource(2000).Token;
				await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

				if (client.Connected)
				{
					Log.Information("Successfully connected to instance");
				}
				else
				{
					Log.Error("Cannot connect to {host}:{port}. Please ensure the service is running.", host, port);
					Log.Warning("You can update ServiceUrl in appsettings.json to point to a valid endpoint.");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to connect to {host}:{port}", host, port);
				Log.Warning("Please ensure the service is installed and running, or update ServiceUrl in appsettings.json");
			}
		}

		private async Task TryRemoteConnectionAsync(string host)
		{
			try
			{
				var success = await _pingService.PingAsync(host, 2000).ConfigureAwait(false);
				if (success)
				{
					Log.Information("Successfully pinged {host}", host);
				}
				else
				{
					Log.Warning("Could not ping {host}", host);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error pinging {host}", host);
			}
		}
	}
}