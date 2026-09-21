﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;


	public sealed class DomResourceNotFoundJobValidatorError : DomResourceNotFoundJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = DomResourceNotFoundJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="DomResourceNotFoundJobValidatorError"/> class.</summary>
		public DomResourceNotFoundJobValidatorError(Guid resourceId, string nodeId)
			: base(resourceId, nodeId)
		{
		}
	}
}
