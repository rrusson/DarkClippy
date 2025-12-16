namespace ClippyWeb.Util
{
    /// <summary>
    /// Abstraction for sending network pings.
    /// </summary>
    public interface IPingService
    {
        Task<bool> PingAsync(string host, int timeoutMs);
    }
}