using System.Configuration;

namespace TestConsole
{
	internal class Program
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

			var semanticClient = new SemanticKernelHelper.SemanticKernelClient(serviceUrl, model, _apiKey: "");
			string? responseX = Task.Run(async () => await semanticClient.GetChatResponseAsync(question)).GetAwaiter().GetResult();
			Console.WriteLine("Semantic Kernel sez:" + responseX + Environment.NewLine);

			//string lmStudioUrl = "http://localhost:1234/v1";
			//string lmStudioModel = "SanctumAI/Meta-Llama-3.1-8B-Instruct-GGUF";
			//var ollamaClient = new LocalAiService.ChatClient(lmStudioUrl, lmStudioModel);

			//string? response = Task.Run(async () => await ollamaClient.GetChatResponseAsync(question)).GetAwaiter().GetResult();
			//Console.WriteLine("Local AI Service sez:" + response + Environment.NewLine);
		}
	}
}
