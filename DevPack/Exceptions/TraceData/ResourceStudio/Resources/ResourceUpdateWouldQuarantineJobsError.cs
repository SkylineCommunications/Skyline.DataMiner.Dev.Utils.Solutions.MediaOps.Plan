namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when updating a resource would move the jobs using it to quarantine.
	/// </summary>
	public sealed class ResourceUpdateWouldQuarantineJobsError : ResourceInUseError
	{
		/// <summary>
		/// Ids of the jobs that would be moved to quarantine.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
