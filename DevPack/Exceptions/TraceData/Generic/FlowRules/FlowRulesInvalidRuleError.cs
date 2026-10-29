namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when flow rules contain an invalid rule.
	/// </summary>
	/// <seealso cref="FlowRulesDuplicateRuleError"/>
	/// <seealso cref="FlowRulesInvalidMessageError"/>
	/// <seealso cref="FlowRulesTargetInvalidStateError"/>
	/// <seealso cref="FlowRulesTargetNotFoundError"/>
	public class FlowRulesInvalidRuleError : FlowRulesError
	{
		/// <summary>
		/// Gets the direction of the invalid rule.
		/// </summary>
		public FlowRuleDirection Direction { get; internal set; }

		/// <summary>
		/// Gets the type of the target of the invalid rule, or <see langword="null"/> when the rule is a default rule.
		/// </summary>
		public FlowRuleObjectType? TargetType { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the target of the invalid rule, or <see langword="null"/> when the rule is a default rule.
		/// </summary>
		public Guid? TargetId { get; internal set; }
	}
}
