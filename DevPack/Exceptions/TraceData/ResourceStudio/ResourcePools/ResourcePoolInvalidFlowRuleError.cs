namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when a resource pool contains an invalid flow rule.
	/// </summary>
	/// <seealso cref="ResourcePoolDuplicateFlowRuleError"/>
	/// <seealso cref="ResourcePoolFlowRuleInvalidMessageError"/>
	/// <seealso cref="ResourcePoolFlowRuleTargetInvalidStateError"/>
	/// <seealso cref="ResourcePoolFlowRuleTargetNotFoundError"/>
	public class ResourcePoolInvalidFlowRuleError : ResourcePoolError
	{
		/// <summary>
		/// Gets the direction of the invalid flow rule.
		/// </summary>
		public FlowRuleDirection Direction { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the target resource pool of the invalid flow rule, or <see langword="null"/> when the rule is a default rule.
		/// </summary>
		public Guid? TargetResourcePoolId { get; internal set; }
	}
}
