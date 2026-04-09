namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when orchestration settings capacity settings are invalid.
	/// </summary>
	public class OrchestrationSettingsInvalidCapacitySettingsError : OrchestrationSettingsError
	{
		/// <summary>
		/// Gets the unique identifier for the capacity.
		/// </summary>
		public Guid CapacityId { get; set; }
	}
}
