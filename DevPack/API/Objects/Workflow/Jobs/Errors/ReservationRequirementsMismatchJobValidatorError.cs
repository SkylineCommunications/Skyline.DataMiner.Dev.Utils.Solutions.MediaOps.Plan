namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents resolved requirements that no longer match a job reservation.</summary>
	public sealed class ReservationRequirementsMismatchJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J602";

		/// <summary>Initializes a new instance of the <see cref="ReservationRequirementsMismatchJobValidatorError"/> class.</summary>
		public ReservationRequirementsMismatchJobValidatorError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}