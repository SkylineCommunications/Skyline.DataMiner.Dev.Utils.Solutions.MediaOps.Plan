namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when attempting to delete a capacity that is currently in use.
	/// </summary>
	/// <seealso cref="CapacityInUseByJobsError"/>
	/// <seealso cref="CapacityInUseByRecurringJobsError"/>
	/// <seealso cref="CapacityInUseByResourcePoolsError"/>
	/// <seealso cref="CapacityInUseByResourcesError"/>
	/// <seealso cref="CapacityInUseByWorkflowsError"/>
	public class CapacityInUseError : CapacityError
	{
	}
}
