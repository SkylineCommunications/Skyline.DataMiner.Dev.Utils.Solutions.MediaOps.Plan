namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a workflow configuration contains a duplicate workflow name.
	/// </summary>
	/// <remarks>This can only occur when workflows with the same name are provided to a bulk operation.</remarks>
	public sealed class WorkflowDuplicateNameError : WorkflowError
	{
		/// <summary>
		/// Gets the name of the workflow.
		/// </summary>
		public string Name { get; internal set; }
	}
}
