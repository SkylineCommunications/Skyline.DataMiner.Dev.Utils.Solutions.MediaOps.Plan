namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource pool is referenced by one or multiple linked resource pools.
	/// </summary>
	public sealed class ResourcePoolInUseByLinkedPoolsError : ResourcePoolInUseError
	{
		/// <summary>
		/// Ids of the linked resource pools referencing the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> LinkedResourcePoolIds { get; internal set; } = [];
	}
}
