namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capacity is in use by one or multiple Resource Pools.
	/// </summary>
	public class CapacityInUseByResourcePoolsError : CapacityInUseError
	{
		/// <summary>
		/// Ids of the resource pools referencing the capacity.
		/// </summary>
		public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
	}
}
