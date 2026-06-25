namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a reference configured on a job cannot be resolved to an actual value.
	/// </summary>
	public sealed class JobUnresolvedReferenceError : JobError
	{
		/// <summary>
		/// Gets the human-readable description of the reference that could not be resolved.
		/// </summary>
		public string Reference { get; internal set; }
	}
}
