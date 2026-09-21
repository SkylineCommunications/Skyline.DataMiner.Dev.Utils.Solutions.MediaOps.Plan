﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class CoreResourceUnavailableJobValidatorError : CoreResourceUnavailableJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = CoreResourceUnavailableJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="CoreResourceUnavailableJobValidatorError"/> class.</summary>
		public CoreResourceUnavailableJobValidatorError(string resourceName, string mode)
			: base(resourceName, mode)
		{
		}
	}
}
