namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a workflow configuration name already exists.
	/// </summary>
	public sealed class WorkflowNameExistsError : WorkflowError
	{
		/// <summary>
		/// Gets the name of the workflow.
		/// </summary>
		public string Name { get; internal set; }
	}
}
