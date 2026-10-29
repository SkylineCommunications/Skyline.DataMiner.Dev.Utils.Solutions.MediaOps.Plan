namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Describes the objects that are created or updated in the same operation as the flow rules being validated, so an
	/// override can target one of them before it is stored, and their requested state is used instead of the stored one.
	/// </summary>
	internal sealed class FlowRulesValidationContext
	{
		private readonly Dictionary<FlowRuleObject, bool> isDeprecatedByObject = new Dictionary<FlowRuleObject, bool>();

		public static FlowRulesValidationContext Empty => new FlowRulesValidationContext();

		public FlowRulesValidationContext Add(FlowRuleObject flowRuleObject, bool isDeprecated)
		{
			if (flowRuleObject.IsEmpty)
			{
				throw new ArgumentException("The object cannot be empty.", nameof(flowRuleObject));
			}

			isDeprecatedByObject[flowRuleObject] = isDeprecated;
			return this;
		}

		public bool TryGet(FlowRuleObject flowRuleObject, out bool isDeprecated)
		{
			return isDeprecatedByObject.TryGetValue(flowRuleObject, out isDeprecated);
		}
	}
}
