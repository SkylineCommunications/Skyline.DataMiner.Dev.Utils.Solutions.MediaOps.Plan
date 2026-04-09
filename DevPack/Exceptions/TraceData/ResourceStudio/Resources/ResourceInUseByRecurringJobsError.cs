namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a resource is referenced by one or multiple Recurring Jobs.
	/// </summary>
	public class ResourceInUseByRecurringJobsError : ResourceInUseError
	{
		/// <summary>
		/// Ids of the recurring jobs referencing the resource.
		/// </summary>
		public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
	}
}
