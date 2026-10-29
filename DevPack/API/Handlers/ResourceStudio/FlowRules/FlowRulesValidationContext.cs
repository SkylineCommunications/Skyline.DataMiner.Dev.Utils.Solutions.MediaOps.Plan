namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Describes the objects that are created or updated in the same operation as the flow rules being validated, so an
	/// override can target one of them before it is stored, and their requested state is used instead of the stored one.
	/// An object that fails later in the operation, after the flow rules were validated, is marked as dropped so the
	/// overrides that target it are not persisted.
	/// </summary>
	internal sealed class FlowRulesValidationContext
	{
		private readonly Dictionary<FlowRuleObject, bool> isDeprecatedByObject = new Dictionary<FlowRuleObject, bool>();

		private readonly HashSet<FlowRuleObject> droppedObjects = new HashSet<FlowRuleObject>();

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

		public void Drop(FlowRuleObject flowRuleObject)
		{
			if (isDeprecatedByObject.Remove(flowRuleObject))
			{
				droppedObjects.Add(flowRuleObject);
			}
		}

		public bool IsDropped(FlowRuleObject flowRuleObject)
		{
			return droppedObjects.Contains(flowRuleObject);
		}
	}
}
