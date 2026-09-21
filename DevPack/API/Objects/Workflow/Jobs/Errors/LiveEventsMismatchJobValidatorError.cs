namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents resolved values that no longer match scheduled MediaOps Live events.</summary>
	public sealed class LiveEventsMismatchJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J603";

		/// <summary>Initializes a new instance of the <see cref="LiveEventsMismatchJobValidatorError"/> class.</summary>
		public LiveEventsMismatchJobValidatorError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}