namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when flow rules contain more than one default rule for a direction, or more than one override for the same direction and target.
	/// </summary>
	public sealed class FlowRulesDuplicateRuleError : FlowRulesInvalidRuleError
	{
	}
}
