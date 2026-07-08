namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job cannot be stopped because it is currently running in its post-roll.
	/// </summary>
	public sealed class JobRunningInPostRollError : JobError
	{
	}
}
