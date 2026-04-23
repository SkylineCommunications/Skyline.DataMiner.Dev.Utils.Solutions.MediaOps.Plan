namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration text discrete value is referenced by one or multiple jobs.
	/// </summary>
	public sealed class ConfigurationTextDiscreteValueInUseByJobsError : ConfigurationTextDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the jobs referencing the configuration text discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
