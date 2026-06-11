namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the pre-roll start of a job is changed while the job state does not allow that change.
	/// </summary>
	public sealed class JobPreRollStartChangeNotAllowedError : JobTimingChangeNotAllowedError
	{
	}
}
