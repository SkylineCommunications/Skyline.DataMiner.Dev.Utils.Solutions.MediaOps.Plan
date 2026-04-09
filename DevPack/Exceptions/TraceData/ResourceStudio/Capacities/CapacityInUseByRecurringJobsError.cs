namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a capacity is referenced by one or multiple Recurring Jobs.
	/// </summary>
	public class CapacityInUseByRecurringJobsError : CapacityInUseError
	{
		/// <summary>
		/// Ids of the recurring jobs referencing the capacity.
		/// </summary>
		public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
	}
}
