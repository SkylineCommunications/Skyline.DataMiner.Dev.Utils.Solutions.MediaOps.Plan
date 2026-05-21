namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a workflow configuration specifies an invalid post-roll value.
	/// </summary>
	public sealed class WorkflowInvalidPostRollError : WorkflowError
	{
		/// <summary>
		/// Gets the post-roll value of the workflow.
		/// </summary>
		public TimeSpan PostRoll { get; internal set; }
	}
}
