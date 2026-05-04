namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when resource capability settings are invalid.
	/// </summary>
	public sealed class ResourceInvalidCapabilitySettingsError : ResourceError
	{
		/// <summary>
		/// Gets the unique identifier for the capability.
		/// </summary>
		public Guid CapabilityId { get; internal set; }
	}
}
