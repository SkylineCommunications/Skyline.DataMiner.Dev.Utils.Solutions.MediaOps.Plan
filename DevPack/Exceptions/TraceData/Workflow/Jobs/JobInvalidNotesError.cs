namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job configuration specifies invalid notes.
	/// </summary>
	public sealed class JobInvalidNotesError : JobError
	{
		/// <summary>
		/// Gets the notes of the job.
		/// </summary>
		public string Notes { get; internal set; }
	}
}
