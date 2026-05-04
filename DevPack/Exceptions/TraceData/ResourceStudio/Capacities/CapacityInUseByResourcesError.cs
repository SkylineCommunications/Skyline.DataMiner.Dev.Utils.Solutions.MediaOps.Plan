namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capacity is referenced by one or multiple resources.
	/// </summary>
	public sealed class CapacityInUseByResourcesError : CapacityInUseError
	{
		/// <summary>
		/// Ids of the resources referencing the capacity.
		/// </summary>
		public IReadOnlyCollection<Guid> ResourceIds { get; internal set; } = new List<Guid>();
	}
}
