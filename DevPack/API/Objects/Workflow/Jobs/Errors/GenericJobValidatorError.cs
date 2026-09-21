﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;


	public sealed class GenericJobValidatorError : GenericJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = GenericJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="GenericJobValidatorError"/> class.</summary>
		public GenericJobValidatorError(Exception exception)
			: base(exception)
		{
		}
	}
}
