namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a configuration is referenced by one or multiple objects in MediaOps.
	/// </summary>
	/// <seealso cref="ConfigurationInUseByJobsError"/>
	/// <seealso cref="ConfigurationInUseByRecurringJobsError"/>
	/// <seealso cref="ConfigurationInUseByResourcePoolsError"/>
	/// <seealso cref="ConfigurationInUseByWorkflowsError"/>
	public class ConfigurationInUseError : ConfigurationError
	{
	}
}
