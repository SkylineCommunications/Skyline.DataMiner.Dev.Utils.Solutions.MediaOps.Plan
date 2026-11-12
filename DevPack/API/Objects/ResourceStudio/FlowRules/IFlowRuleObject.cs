namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an object that can own flow rules or be the target of a flow rule.
	/// </summary>
	public interface IFlowRuleObject
	{
		/// <summary>
		/// Gets the unique identifier of the object.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Gets the type of the object, as used by flow rules.
		/// </summary>
		FlowRuleObjectType FlowRuleObjectType { get; }
	}
}
