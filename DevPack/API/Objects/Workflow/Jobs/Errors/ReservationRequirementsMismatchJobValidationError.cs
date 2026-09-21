namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents resolved requirements that no longer match a job reservation.</summary>
	public class ReservationRequirementsMismatchJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J602";

		/// <summary>Initializes a new instance of the <see cref="ReservationRequirementsMismatchJobValidationError"/> class.</summary>
		public ReservationRequirementsMismatchJobValidationError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}