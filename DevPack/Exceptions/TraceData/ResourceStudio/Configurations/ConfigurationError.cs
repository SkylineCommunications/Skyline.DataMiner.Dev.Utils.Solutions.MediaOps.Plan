namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a configuration with invalid configuration.
	/// </summary>
	public class ConfigurationError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the configuration.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
