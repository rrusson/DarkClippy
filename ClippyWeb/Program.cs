using SharedInterfaces;

using Serilog;
using Serilog.Events;

using ClippyWeb.Util;

namespace ClippyWeb
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
			SetupLogging(builder.Configuration);

			try
			{
				builder.Host.UseSerilog();

				// Enable reflection-based serialization for System.Text.Json
				// This is required for the OpenAI library to work correctly in production
				// Without this, you'll get: "System.InvalidOperationException: Reflection-based serialization has been disabled for this application"
				AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);

				builder.Services.AddControllers().AddNewtonsoftJson();
				builder.Services.AddRazorPages();
				builder.Services.ConfigureHttpJsonOptions(options =>
				{
					options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
				});

				ConnectionValidator.ValidateConnection(builder.Configuration);

				builder.Services.AddSingleton<IChatClient>(provider =>
				{
					string? serviceUrl = builder.Configuration["ServiceUrl"];
					if (string.IsNullOrEmpty(serviceUrl))
					{
						throw new InvalidOperationException("Please supply a config value for ServiceUrl.");
					}

					string? model = builder.Configuration["Model"];
					if (string.IsNullOrEmpty(model))
					{
						throw new InvalidOperationException("Please supply a config value for Model.");
					}

					Log.Information($"Connecting to LLM service at: {serviceUrl} with model: {model}");

					//return new LocalAiService.ChatClient>(serviceUrl, model);
					return new SemanticKernelHelper.SemanticKernalClient(serviceUrl, model);
				});

				var app = builder.Build();

				if (!app.Environment.IsDevelopment())
				{
					app.UseExceptionHandler("/Error");
					app.UseHsts();
				}

				//app.UseHttpsRedirection();
				app.UseStaticFiles();
				app.UseRouting();
				app.UseAuthorization();

				app.MapControllers();
				app.MapRazorPages();

				Log.Information("Application starting up");
				app.Run();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "Application start-up failed");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static void SetupLogging(IConfiguration configuration)
		{
			string logPath = configuration["LogPath"] ?? "C:\\temp\\clippyLog\\";

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
