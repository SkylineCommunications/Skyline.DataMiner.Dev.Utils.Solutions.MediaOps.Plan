namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that is associated with a specific resource within a job operation.
	/// </summary>
	public class JobResourceError : JobError
	{
		/// <summary>
		/// Gets the unique identifier for the associated resource.
		/// </summary>
		public Guid ResourceId { get; internal set; }
	}
}
