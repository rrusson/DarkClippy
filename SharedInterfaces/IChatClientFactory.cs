namespace SharedInterfaces
{
	public interface IChatClientFactory
	{
		Task<IChatClient> GetOrCreateClientAsync(string sessionKey);
	}
}
