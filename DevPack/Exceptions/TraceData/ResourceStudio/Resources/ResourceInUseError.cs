namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource cannot be deleted or changed because it is currently in use.
	/// </summary>
	/// <seealso cref="ResourceInUseByJobsError"/>
	/// <seealso cref="ResourceInUseByRecurringJobsError"/>
	/// <seealso cref="ResourceInUseByWorkflowsError"/>
	/// <seealso cref="ResourceUpdateWouldQuarantineJobsError"/>
	public class ResourceInUseError : ResourceError
	{
	}
}
