namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a difference between the resource pools a resource belongs to in DOM and the ones it belongs to in CORE.
	/// </summary>
	public sealed class ResourcePoolMembershipDifference : SynchronizationDifference
	{
		internal ResourcePoolMembershipDifference(SynchronizationDifferenceKind kind, Guid coreResourcePoolId)
			: base(kind)
		{
			CoreResourcePoolId = coreResourcePoolId;
		}

		/// <summary>
		/// Gets the identifier of the CORE resource pool the membership applies to.
		/// </summary>
		public Guid CoreResourcePoolId { get; }

		/// <summary>
		/// Gets the name of the resource pool, or <see langword="null"/> when the pool is only known to CORE.
		/// </summary>
		public string Name { get; internal set; }
	}
}
