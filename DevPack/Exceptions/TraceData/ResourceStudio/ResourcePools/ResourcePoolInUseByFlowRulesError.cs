namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource pool is targeted by flow rule overrides of one or multiple other objects.
	/// </summary>
	public sealed class ResourcePoolInUseByFlowRulesError : ResourcePoolInUseError
	{
		/// <summary>
		/// Gets the IDs of the objects whose flow rules reference the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> ReferencingOwnerIds { get; internal set; } = [];
	}
}
