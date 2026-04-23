namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration text discrete value is referenced by one or multiple Recurring Jobs.
	/// </summary>
	public sealed class ConfigurationTextDiscreteValueInUseByRecurringJobsError : ConfigurationTextDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the recurring jobs referencing the configuration text discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
	}
}
