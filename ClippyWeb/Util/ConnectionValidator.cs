using Serilog;

using System.Net.NetworkInformation;

namespace ClippyWeb.Util
{
	public static class ConnectionValidator
	{
		public static void ValidateConnection(IConfiguration configuration)
		{
			string? serviceUrl = configuration["ServiceUrl"];
			if (string.IsNullOrEmpty(serviceUrl))
			{
				Log.Warning("ServiceUrl not configured in appsettings.json");
				return;
			}

			Uri uri = new Uri(serviceUrl);
			string host = uri.Host;
			int port = uri.Port;

			Log.Information($"Validating connection to {host}:{port}");

			if (host == "localhost" || host == "127.0.0.1")
			{
				TryLocalConnection(host, port);
			}
			else
			{
				TryRemoteConnection(host);
			}
		}

		private static void TryLocalConnection(string host, int port)
		{
			try
			{
				using var client = new System.Net.Sockets.TcpClient();
				var connectTask = client.ConnectAsync(host, port);
				var completedTask = Task.WhenAny(connectTask, Task.Delay(2000)).Result;

				if (completedTask == connectTask && client.Connected)
				{
					Log.Information("Successfully connected to instance");
				}
				else
				{
					Log.Error($"Cannot connect to {host}:{port}. Please ensure the service is running.");
					Log.Warning("You can update ServiceUrl in appsettings.json to point to a valid endpoint.");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Failed to connect to {host}:{port}");
				Log.Warning("Please ensure the service is installed and running, or update ServiceUrl in appsettings.json");
			}
		}

		private static void TryRemoteConnection(string host)
		{
			Ping ping = new Ping();
			try
			{
				PingReply reply = ping.Send(host, 2000);
				if (reply.Status == IPStatus.Success)
				{
					Log.Information($"Successfully pinged {host}, round-trip time: {reply.RoundtripTime}ms");
				}
				else
				{
					Log.Warning($"Could not ping {host}: {reply.Status}");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Error pinging {host}");
			}
		}
	}
}