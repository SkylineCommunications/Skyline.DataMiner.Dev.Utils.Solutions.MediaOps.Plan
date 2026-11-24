namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource cannot be unassigned from a resource pool because one or multiple workflows use the resource from that pool.
	/// </summary>
	public sealed class ResourcePoolAssignmentInUseByWorkflowsError : ResourceInUseError
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool from which the resource cannot be unassigned.
		/// </summary>
		public Guid ResourcePoolId { get; internal set; }

		/// <summary>
		/// Ids of the workflows using the resource from the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
	}
}
