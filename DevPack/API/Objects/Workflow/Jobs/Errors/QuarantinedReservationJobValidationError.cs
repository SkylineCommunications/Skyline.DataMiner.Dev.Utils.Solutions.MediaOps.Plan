namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a job reservation that contains quarantined resources.</summary>
	public sealed class QuarantinedReservationJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J105";

		/// <summary>Initializes a new instance of the <see cref="QuarantinedReservationJobValidationError"/> class.</summary>
		public QuarantinedReservationJobValidationError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}