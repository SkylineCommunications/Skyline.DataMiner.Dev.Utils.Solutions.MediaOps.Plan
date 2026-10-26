namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource pool contains more than one default rule for a direction, or more than one override for the same direction and target resource pool.
	/// </summary>
	public sealed class ResourcePoolDuplicateFlowRuleError : ResourcePoolInvalidFlowRuleError
	{
	}
}
