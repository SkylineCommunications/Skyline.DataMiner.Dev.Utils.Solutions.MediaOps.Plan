namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>Represents a MediaOps Live virtual signal group that could not be found.</summary>
	public sealed class VirtualSignalGroupNotFoundJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J201";

		/// <summary>Initializes a new instance of the <see cref="VirtualSignalGroupNotFoundJobValidatorError"/> class.</summary>
		public VirtualSignalGroupNotFoundJobValidatorError(string direction, Guid virtualSignalGroupId, string resourceName)
			: base(ErrorCode, $"Couldn't find ({direction}) virtual signal group with ID '{virtualSignalGroupId}' for resource {resourceName}")
		{
		}
	}
}