namespace SharedInterfaces
{
	public interface IChatClient
	{
		public Task<string?> GetChatResponseAsync(string chatMessage);
	}
}
