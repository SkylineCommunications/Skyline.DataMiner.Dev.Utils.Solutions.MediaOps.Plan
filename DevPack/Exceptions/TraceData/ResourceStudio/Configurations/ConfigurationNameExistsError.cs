namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a configuration configuration specifies an invalid configuration name.
	/// </summary>
	public sealed class ConfigurationNameExistsError : ConfigurationError
	{
		/// <summary>
		/// Gets the name of the configuration.
		/// </summary>
		public string Name { get; internal set; }
	}
}
