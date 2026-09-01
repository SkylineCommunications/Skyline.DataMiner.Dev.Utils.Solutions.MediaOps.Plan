namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when attempting to delete a capability that is currently in use.
	/// </summary>
	/// <seealso cref="CapabilityInUseByJobsError"/>
	/// <seealso cref="CapabilityInUseByRecurringJobsError"/>
	/// <seealso cref="CapabilityInUseByResourcePoolsError"/>
	/// <seealso cref="CapabilityInUseByResourcesError"/>
	/// <seealso cref="CapabilityInUseByWorkflowsError"/>
	public class CapabilityInUseError : CapabilityError
	{
	}
}
