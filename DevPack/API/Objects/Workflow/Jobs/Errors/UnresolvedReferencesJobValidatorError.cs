﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class UnresolvedReferencesJobValidatorError : UnresolvedReferencesJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = UnresolvedReferencesJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="UnresolvedReferencesJobValidatorError"/> class.</summary>
		public UnresolvedReferencesJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
