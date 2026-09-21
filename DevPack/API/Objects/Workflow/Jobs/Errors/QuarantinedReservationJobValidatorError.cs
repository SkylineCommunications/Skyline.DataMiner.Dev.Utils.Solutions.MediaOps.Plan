﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class QuarantinedReservationJobValidatorError : QuarantinedReservationJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = QuarantinedReservationJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="QuarantinedReservationJobValidatorError"/> class.</summary>
		public QuarantinedReservationJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
