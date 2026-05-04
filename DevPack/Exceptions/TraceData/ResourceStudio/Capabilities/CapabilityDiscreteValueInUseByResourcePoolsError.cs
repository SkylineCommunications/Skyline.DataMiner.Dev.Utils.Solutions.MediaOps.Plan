namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capability discrete value is in use by one or multiple Resource Pools.
	/// </summary>
	public sealed class CapabilityDiscreteValueInUseByResourcePoolsError : CapabilityDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the resource pools referencing the capability discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
	}
}
