namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a configuration configuration contains a duplicate configuration name.
	/// </summary>
	/// <remarks>This can only occur when configurations with the same name are provided to a bulk operation.</remarks>
	public sealed class ConfigurationDuplicateNameError : ConfigurationError
	{
		/// <summary>
		/// Gets the name of the configuration.
		/// </summary>
		public string Name { get; internal set; }
	}
}
