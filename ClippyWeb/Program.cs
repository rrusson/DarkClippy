using System.Diagnostics.CodeAnalysis;

using ClippyWeb.Util;

using Microsoft.Extensions.Caching.Memory;

using Serilog;
using Serilog.Events;

using SharedInterfaces;


namespace ClippyWeb
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);
			builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
			LoggingSetup.SetupLogging(builder.Configuration);

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

				await LlmSetup.SetupLlmService(builder);

				var app = builder.Build();

				if (!app.Environment.IsDevelopment())
				{
					app.UseExceptionHandler("/Error");
					app.UseHsts();
				}

				app.UseStaticFiles();
				app.UseRouting();
				app.UseAuthorization();

				app.MapControllers();
				app.MapRazorPages();

				Log.Information("DarkClippy: Application starting up");
				await app.RunAsync();
			}
			catch (Exception ex)
			{
				Log.Fatal(ex, "DarkClippy: Application start-up failed");
			}
			finally
			{
				await Log.CloseAndFlushAsync();
			}
		}
	}
}
