namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration number discrete value is referenced by one or multiple Recurring Jobs.
	/// </summary>
	public sealed class ConfigurationNumberDiscreteValueInUseByRecurringJobsError : ConfigurationNumberDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the recurring jobs referencing the configuration number discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
	}
}
