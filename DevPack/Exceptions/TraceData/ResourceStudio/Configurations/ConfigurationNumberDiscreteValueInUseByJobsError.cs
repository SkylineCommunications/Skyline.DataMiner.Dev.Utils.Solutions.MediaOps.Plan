namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an error that occurs when a configuration number discrete value is referenced by one or multiple jobs.
	/// </summary>
	public class ConfigurationNumberDiscreteValueInUseByJobsError : ConfigurationNumberDiscreteValueInUseError
	{
		/// <summary>
		/// Ids of the jobs referencing the configuration number discrete value.
		/// </summary>
		public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
	}
}
