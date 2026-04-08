namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when resource capacity settings are invalid.
	/// </summary>
	public class ResourceInvalidCapacitySettingsError : ResourceError
	{
		/// <summary>
		/// Gets the unique identifier for the capacity.
		/// </summary>
		public Guid CapacityId { get; set; }
	}
}
