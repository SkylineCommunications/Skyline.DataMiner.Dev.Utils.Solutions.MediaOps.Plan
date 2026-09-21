﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{

	public sealed class DomResourceInvalidJobValidatorError : DomResourceInvalidJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = DomResourceInvalidJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="DomResourceInvalidJobValidatorError"/> class.</summary>
		public DomResourceInvalidJobValidatorError(string message)
			: base(message)
		{
		}
	}
}
