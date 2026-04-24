namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capability discrete value is referenced by one or multiple workflows.
	/// </summary>
	public sealed class CapabilityDiscreteValueInUseByWorkflowsError : CapabilityDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the workflows referencing the capability discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> WorkflowIds { get; internal set; } = new List<Guid>();
	}
}
