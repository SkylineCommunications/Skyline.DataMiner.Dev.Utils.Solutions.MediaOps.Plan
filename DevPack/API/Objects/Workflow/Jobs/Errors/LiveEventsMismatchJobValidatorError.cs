﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class LiveEventsMismatchJobValidatorError : LiveEventsMismatchJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = LiveEventsMismatchJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="LiveEventsMismatchJobValidatorError"/> class.</summary>
		public LiveEventsMismatchJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
