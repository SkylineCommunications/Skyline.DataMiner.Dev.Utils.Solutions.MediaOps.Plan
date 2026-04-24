namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource pool is referenced by one or multiple Recurring Jobs.
	/// </summary>
	public sealed class ResourcePoolInUseByRecurringJobsError : ResourcePoolInUseError
	{
		/// <summary>
		/// Ids of the recurring jobs referencing the resource pool.
		/// </summary>
		public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
	}
}
