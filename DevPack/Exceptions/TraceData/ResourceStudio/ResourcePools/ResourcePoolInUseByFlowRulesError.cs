namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource pool is targeted by flow rule overrides of one or multiple other resource pools.
	/// </summary>
	public sealed class ResourcePoolInUseByFlowRulesError : ResourcePoolInUseError
	{
		/// <summary>
		/// Gets the IDs of the resource pools whose flow rules reference the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> ReferencingResourcePoolIds { get; internal set; } = [];
	}
}
