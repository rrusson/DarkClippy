using System.Diagnostics.CodeAnalysis;

using Serilog;
using Serilog.Events;

namespace ClippyWeb
{
	internal static class LoggingSetup
	{
		/// <summary>
		/// Configures the logging system using configuration settings
		/// </summary>
		/// <param name="configuration">The configuration manager containing application settings, including the logging directory path.</param>
		/// <exception cref="System.Configuration.ConfigurationErrorsException">Thrown if the logging directory path setting is missing from the configuration.</exception>
		/// <remarks>This method sets up Serilog to log to both the console and a rolling file in the specified directory.
		/// The log file is rotated daily and limited in size and retention. Logging levels for Microsoft and ASP.NET Core components are set to warning or higher.</remarks>
		[ExcludeFromCodeCoverage]
		internal static void SetupLogging(ConfigurationManager configuration)
		{
			string logPath = configuration["LogPath"] ?? throw new System.Configuration.ConfigurationErrorsException("Logging directory path setting missing from appsettings.");

			if (!Directory.Exists(logPath))
			{
				try
				{
					Directory.CreateDirectory(logPath);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Failed to create log directory at '{logPath}'. See inner exception for details.", ex);
				}
			}

			// Validate that the directory is writable
			try
			{
				string testFilePath = Path.Combine(logPath, Path.GetRandomFileName());
				using (FileStream fs = File.Create(testFilePath, 1, FileOptions.DeleteOnClose))
				{
					// Successfully created and will delete on close
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"The log directory '{logPath}' is not writable. Please check permissions. See inner exception for details.", ex);
			}
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Information()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
				.WriteTo.Console()
				.WriteTo.File(@$"{logPath}clippy.log",
					rollingInterval: RollingInterval.Day,
					fileSizeLimitBytes: 10 * 1024 * 1024,
					retainedFileCountLimit: 30,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
				.CreateLogger();
		}
	}
}