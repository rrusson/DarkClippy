namespace SharedInterfaces
{
	public interface IChatClientFactory
	{
		IChatClient GetOrCreateClient(string sessionKey);
	}
}
