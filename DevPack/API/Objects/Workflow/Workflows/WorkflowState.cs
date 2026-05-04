namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the state of a workflow.
	/// </summary>
	public enum WorkflowState
	{
		/// <summary>
		/// The workflow is in draft state.
		/// </summary>
		Draft,

		/// <summary>
		/// The workflow is in complete state.
		/// </summary>
		Complete,

		/// <summary>
		/// The workflow is in obsolete state.
		/// </summary>
		Obsolete,
	}
}
