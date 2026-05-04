namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capability is in use by one or multiple Resource Pools.
	/// </summary>
	public sealed class CapabilityInUseByResourcePoolsError : CapabilityInUseError
	{
		/// <summary>
		/// Ids of the resource pools referencing the capability.
		/// </summary>
		public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
	}
}
