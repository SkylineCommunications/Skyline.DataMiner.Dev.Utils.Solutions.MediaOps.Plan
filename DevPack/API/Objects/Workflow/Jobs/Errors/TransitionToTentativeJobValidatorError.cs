namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a recurring job occurrence that could not transition to tentative.</summary>
	public sealed class TransitionToTentativeJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J501";

		/// <summary>Initializes a new instance of the <see cref="TransitionToTentativeJobValidatorError"/> class.</summary>
		public TransitionToTentativeJobValidatorError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}