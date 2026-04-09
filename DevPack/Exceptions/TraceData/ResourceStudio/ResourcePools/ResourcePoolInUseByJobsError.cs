namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource pool is referenced by one or multiple jobs.
	/// </summary>
	public class ResourcePoolInUseByJobsError : ResourcePoolInUseError
	{
		/// <summary>
		/// Ids of the jobs referencing the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
