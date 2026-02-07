namespace SharedInterfaces
{
	/// <summary>
	/// Interface for detecting model capabilities such as tool/function calling support.
	/// </summary>
	public interface IModelCapabilityDetector
	{
		/// <summary>
		/// Checks if the specified model supports tool/function calling.
		/// </summary>
		/// <param name="modelName">The name of the model to check.</param>
		/// <returns>True if the model supports tools/function calling, false otherwise.</returns>
		Task<bool> SupportsToolsAsync(string modelName);
	}
}
