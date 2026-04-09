namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a workflow with invalid configuration.
	/// </summary>
	public class WorkflowError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the workflow.
		/// </summary>
		public Guid Id { get; set; }
	}
}
