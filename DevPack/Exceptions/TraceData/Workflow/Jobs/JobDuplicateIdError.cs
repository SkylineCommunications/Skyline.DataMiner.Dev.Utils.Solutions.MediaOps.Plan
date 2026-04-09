namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a jobs configuration contains duplicate identifiers.
	/// </summary>
	/// <remarks>This can only occur when jobs with the same ID are provided to a bulk operation.</remarks>
	public class JobDuplicateIdError : JobError
	{
	}
}
