﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class ReservationRequirementsMismatchJobValidatorError : ReservationRequirementsMismatchJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = ReservationRequirementsMismatchJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="ReservationRequirementsMismatchJobValidatorError"/> class.</summary>
		public ReservationRequirementsMismatchJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
