namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents resolved values that no longer match scheduled MediaOps Live events.</summary>
	public sealed class LiveEventsMismatchJobError : JobError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J603";

		/// <summary>Initializes a new instance of the <see cref="LiveEventsMismatchJobError"/> class.</summary>
		public LiveEventsMismatchJobError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}