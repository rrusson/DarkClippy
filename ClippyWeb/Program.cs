using SharedInterfaces;

using System.Configuration;
using System.Reflection;

using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace ClippyWeb
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddControllers();

			// Add Razor Pages services
			builder.Services.AddRazorPages();

			// Add NewtonSoftJson for JSON serialization
			builder.Services.AddControllers().AddNewtonsoftJson();

			builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			// Add the singleton services before building the app
			builder.Services.AddSingleton<IChatClient>(provider =>
			{
				string? serviceUrl = builder.Configuration["ServiceUrl"];
				if (string.IsNullOrEmpty(serviceUrl))
				{
					throw new System.Configuration.ConfigurationErrorsException("Please supply a config value for ServiceUrl.");
				}

				string? model = builder.Configuration["Model"];
				if (string.IsNullOrEmpty(model))
				{
					throw new ConfigurationErrorsException("Please supply a config value for Model.");
				}

				//return new LocalAiService.ChatClient>(serviceUrl, model);
				return new SemanticKernelHelper.SemanticKernalClient(serviceUrl, model);
			});

			var app = builder.Build();

			// Configure the HTTP request pipeline.
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

			app.Run();
		}
	}
}
