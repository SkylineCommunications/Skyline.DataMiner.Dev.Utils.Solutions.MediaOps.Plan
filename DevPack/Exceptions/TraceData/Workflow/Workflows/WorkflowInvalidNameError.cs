namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a workflow configuration specifies an invalid workflow name.
	/// </summary>
	public sealed class WorkflowInvalidNameError : WorkflowError
	{
		/// <summary>
		/// Gets the name of the workflow.
		/// </summary>
		public string Name { get; internal set; }
	}
}
