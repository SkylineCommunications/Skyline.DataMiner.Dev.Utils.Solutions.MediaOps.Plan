namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when the recurring job referenced by <see cref="Skyline.DataMiner.Solutions.MediaOps.Plan.API.Job.RecurringJobId"/>
	/// cannot be found in the system.
	/// </summary>
	public sealed class JobRecurringJobNotFoundError : JobError
	{
		/// <summary>
		/// Gets the unique identifier of the referenced recurring job that could not be found.
		/// </summary>
		public Guid RecurringJobId { get; internal set; }
	}
}
