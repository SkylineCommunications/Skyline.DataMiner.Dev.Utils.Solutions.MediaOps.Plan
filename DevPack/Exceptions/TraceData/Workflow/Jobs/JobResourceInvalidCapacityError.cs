namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when attempting to configure a capacity that is not available on the resource.
	/// </summary>
	public class JobResourceInvalidCapacityError : JobResourceError
	{
		/// <summary>
		/// Gets the unique identifier for the capacity.
		/// </summary>
		public Guid CapacityId { get; internal set; }
	}
}
