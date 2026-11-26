namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource cannot be unassigned from a resource pool because one or multiple jobs use the resource from that pool.
	/// </summary>
	public sealed class ResourcePoolAssignmentInUseByJobsError : ResourceInUseError
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool from which the resource cannot be unassigned.
		/// </summary>
		public Guid ResourcePoolId { get; internal set; }

		/// <summary>
		/// Ids of the jobs using the resource from the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
