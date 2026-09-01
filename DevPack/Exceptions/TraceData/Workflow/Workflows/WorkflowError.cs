namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a workflow with invalid configuration.
	/// </summary>
	/// <seealso cref="WorkflowDuplicateIdError"/>
	/// <seealso cref="WorkflowDuplicateNameError"/>
	/// <seealso cref="WorkflowIdInUseError"/>
	/// <seealso cref="WorkflowInvalidNameError"/>
	/// <seealso cref="WorkflowInvalidPostRollError"/>
	/// <seealso cref="WorkflowInvalidPreRollError"/>
	/// <seealso cref="WorkflowInvalidStateError"/>
	/// <seealso cref="WorkflowNameExistsError"/>
	/// <seealso cref="WorkflowNodeGraphError"/>
	/// <seealso cref="WorkflowNotFoundError"/>
	/// <seealso cref="WorkflowValueAlreadyChangedError"/>
	public class WorkflowError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the workflow.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
