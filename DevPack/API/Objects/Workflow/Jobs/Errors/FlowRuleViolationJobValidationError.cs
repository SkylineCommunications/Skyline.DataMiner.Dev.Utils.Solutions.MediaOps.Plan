namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents connections of a job that are forbidden by flow rules.</summary>
	public sealed class FlowRuleViolationJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J701";

		/// <summary>Initializes a new instance of the <see cref="FlowRuleViolationJobValidationError"/> class.</summary>
		public FlowRuleViolationJobValidationError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}
