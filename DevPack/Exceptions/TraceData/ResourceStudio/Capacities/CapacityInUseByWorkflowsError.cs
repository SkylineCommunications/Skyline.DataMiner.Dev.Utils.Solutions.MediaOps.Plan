namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capacity is referenced by one or multiple workflows.
	/// </summary>
	public sealed class CapacityInUseByWorkflowsError : CapacityInUseError
	{
		/// <summary>
		/// Ids of the workflows referencing the capacity.
		/// </summary>
		public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
	}
}
