using Microsoft.Extensions.Configuration;

namespace SharedInterfaces
{
    /// <summary>
    /// Validates application connection settings.
    /// </summary>
    public interface IConnectionValidator
    {
        /// <summary>
        /// Validates the application's connection settings using configuration.
        /// </summary>
        /// <param name="configuration">The configuration source containing connection settings.</param>
        Task ValidateConnectionAsync(IConfiguration configuration);
    }
}