﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class TransitionToTentativeJobValidatorError : TransitionToTentativeJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = TransitionToTentativeJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="TransitionToTentativeJobValidatorError"/> class.</summary>
		public TransitionToTentativeJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
