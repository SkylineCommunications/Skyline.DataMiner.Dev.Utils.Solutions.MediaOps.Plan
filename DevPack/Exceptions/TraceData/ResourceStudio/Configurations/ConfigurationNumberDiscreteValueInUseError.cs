namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when attempting to delete a configuration Number discrete value that is currently in use.
	/// </summary>
	/// <seealso cref="ConfigurationNumberDiscreteValueInUseByJobsError"/>
	/// <seealso cref="ConfigurationNumberDiscreteValueInUseByRecurringJobsError"/>
	/// <seealso cref="ConfigurationNumberDiscreteValueInUseByResourcePoolsError"/>
	/// <seealso cref="ConfigurationNumberDiscreteValueInUseByWorkflowsError"/>
	public class ConfigurationNumberDiscreteValueInUseError : ConfigurationError
	{
		/// <summary>
		/// The discrete value that is in use.
		/// </summary>
		public NumberDiscreet DiscreteValue { get; internal set; }
	}
}
