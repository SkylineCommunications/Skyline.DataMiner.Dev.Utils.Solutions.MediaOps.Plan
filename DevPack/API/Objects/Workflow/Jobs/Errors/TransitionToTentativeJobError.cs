namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a recurring job occurrence that could not transition to tentative.</summary>
	public sealed class TransitionToTentativeJobError : JobError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J501";

		/// <summary>Initializes a new instance of the <see cref="TransitionToTentativeJobError"/> class.</summary>
		public TransitionToTentativeJobError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}