﻿namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;


	public sealed class VirtualSignalGroupNotFoundJobValidatorError : VirtualSignalGroupNotFoundJobValidationError
	{
		/// <summary>The error code.</summary>
		public new const string ErrorCode = VirtualSignalGroupNotFoundJobValidationError.ErrorCode;

		/// <summary>Initializes a new instance of the <see cref="VirtualSignalGroupNotFoundJobValidatorError"/> class.</summary>
		public VirtualSignalGroupNotFoundJobValidatorError(string direction, Guid virtualSignalGroupId, string resourceName)
			: base(direction, virtualSignalGroupId, resourceName)
		{
		}
	}
}
