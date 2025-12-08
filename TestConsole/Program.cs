using System.Configuration;

namespace TestConsole
{
	internal static class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Hello, World! Ask me a question.\n\rQ:");
			string question = Console.ReadLine() ?? "How do I know if something is important?";

			string? serviceUrl = ConfigurationManager.AppSettings["ServiceUrl"];
			string? model = ConfigurationManager.AppSettings["Model"];

			if (string.IsNullOrEmpty(serviceUrl))
			{
				throw new ConfigurationErrorsException("Please supply a config value for ServiceUrl.");
			}

			if (string.IsNullOrEmpty(model))
			{
				throw new ConfigurationErrorsException("Please supply a config value for Model.");
			}

			var semanticClient = new SemanticKernelHelper.SemanticKernelClient(serviceUrl, model);
			string? responseX = Task.Run(async () => await semanticClient.GetChatResponseAsync(question)).GetAwaiter().GetResult();
			Console.WriteLine("Semantic Kernel sez:" + responseX + Environment.NewLine);
		}
	}
}
