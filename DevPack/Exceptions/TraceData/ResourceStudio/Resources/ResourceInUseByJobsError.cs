namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource is referenced by one or multiple jobs.
	/// </summary>
	public sealed class ResourceInUseByJobsError : ResourceInUseError
	{
		/// <summary>
		/// Ids of the jobs referencing the resource.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
