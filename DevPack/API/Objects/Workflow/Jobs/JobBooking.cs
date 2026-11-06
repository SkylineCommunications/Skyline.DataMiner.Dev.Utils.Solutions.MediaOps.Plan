namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the booking of a job: the time range and the resource usage of its nodes as they are booked.
	/// </summary>
	public sealed class JobBooking
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobBooking"/> class.
		/// </summary>
		/// <param name="jobId">The identifier of the job.</param>
		/// <param name="start">The start of the booked time range.</param>
		/// <param name="end">The end of the booked time range.</param>
		/// <param name="usesResources">Whether the booking currently uses its resources.</param>
		/// <param name="nodes">The booked resource usage of the nodes.</param>
		public JobBooking(Guid jobId, DateTimeOffset start, DateTimeOffset end, bool usesResources, IEnumerable<JobNodeResourceUsage> nodes)
		{
			JobId = jobId;
			Start = start;
			End = end;
			UsesResources = usesResources;
			Nodes = (nodes ?? Enumerable.Empty<JobNodeResourceUsage>()).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the identifier of the job.
		/// </summary>
		public Guid JobId { get; }

		/// <summary>
		/// Gets the start of the booked time range.
		/// </summary>
		public DateTimeOffset Start { get; }

		/// <summary>
		/// Gets the end of the booked time range.
		/// </summary>
		public DateTimeOffset End { get; }

		/// <summary>
		/// Gets a value indicating whether the booking currently uses its resources. A canceled or ended booking does not.
		/// </summary>
		public bool UsesResources { get; }

		/// <summary>
		/// Gets the booked resource usage of the nodes. Quarantined usages are not included.
		/// </summary>
		public IReadOnlyCollection<JobNodeResourceUsage> Nodes { get; }
	}
}
