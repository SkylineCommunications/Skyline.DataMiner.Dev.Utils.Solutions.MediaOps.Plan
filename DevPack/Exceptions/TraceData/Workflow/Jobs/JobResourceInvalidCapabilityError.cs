namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when attempting to configure a capability that is not available on the resource.
	/// </summary>
	public class JobResourceInvalidCapabilityError : JobResourceError
	{
		/// <summary>
		/// Gets the unique identifier for the capability.
		/// </summary>
		public Guid CapabilityId { get; internal set; }
	}
}
