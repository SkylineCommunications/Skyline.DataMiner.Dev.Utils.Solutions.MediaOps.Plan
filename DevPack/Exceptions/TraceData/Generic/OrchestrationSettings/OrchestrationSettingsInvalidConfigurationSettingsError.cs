namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when orchestration settings configuration settings are invalid.
	/// </summary>
	public sealed class OrchestrationSettingsInvalidConfigurationSettingsError : OrchestrationSettingsError
	{
		/// <summary>
		/// Gets the unique identifier for the configuration.
		/// </summary>
		public Guid ConfigurationId { get; internal set; }
	}
}
