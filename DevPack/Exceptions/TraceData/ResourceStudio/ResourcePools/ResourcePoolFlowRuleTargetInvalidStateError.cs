namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a flow rule targets a resource pool that is in a state that does not allow it to be targeted, such as deprecated.
	/// </summary>
	public sealed class ResourcePoolFlowRuleTargetInvalidStateError : ResourcePoolInvalidFlowRuleError
	{
	}
}
