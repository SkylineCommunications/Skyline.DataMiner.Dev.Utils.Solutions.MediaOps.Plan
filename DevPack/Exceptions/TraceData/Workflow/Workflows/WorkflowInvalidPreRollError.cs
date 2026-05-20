namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a workflow configuration specifies an invalid pre-roll value.
	/// </summary>
	public sealed class WorkflowInvalidPreRollError : WorkflowError
	{
		/// <summary>
		/// Gets the pre-roll value of the workflow.
		/// </summary>
		public TimeSpan PreRoll { get; internal set; }
	}
}
