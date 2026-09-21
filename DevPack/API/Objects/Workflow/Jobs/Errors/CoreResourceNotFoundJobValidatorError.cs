﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;


	public sealed class CoreResourceNotFoundJobValidatorError : CoreResourceNotFoundJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = CoreResourceNotFoundJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="CoreResourceNotFoundJobValidatorError"/> class.</summary>
		public CoreResourceNotFoundJobValidatorError(Guid coreResourceId, string resourceName)
			: base(coreResourceId, resourceName)
		{
		}
	}
}
