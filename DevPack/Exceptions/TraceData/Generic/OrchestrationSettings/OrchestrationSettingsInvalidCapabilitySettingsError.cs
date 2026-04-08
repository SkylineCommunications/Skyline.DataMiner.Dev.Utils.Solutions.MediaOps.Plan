namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when orchestration settings capability settings are invalid.
	/// </summary>
	public class OrchestrationSettingsInvalidCapabilitySettingsError : OrchestrationSettingsError
	{
		/// <summary>
		/// Gets the unique identifier for the capability.
		/// </summary>
		public Guid CapabilityId { get; set; }
	}
}
