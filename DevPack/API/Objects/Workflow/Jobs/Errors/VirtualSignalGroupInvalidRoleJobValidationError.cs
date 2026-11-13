namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>Represents a MediaOps Live virtual signal group whose role does not match its usage on the resource.</summary>
	/// <remarks>The input virtual signal group of a resource must be a destination; the output virtual signal group must be a source.</remarks>
	public sealed class VirtualSignalGroupInvalidRoleJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J202";

		/// <summary>Initializes a new instance of the <see cref="VirtualSignalGroupInvalidRoleJobValidationError"/> class.</summary>
		public VirtualSignalGroupInvalidRoleJobValidationError(string direction, Guid virtualSignalGroupId, string actualRole, string expectedRole, string resourceName)
			: base(ErrorCode, $"The ({direction}) virtual signal group with ID '{virtualSignalGroupId}' for resource {resourceName} has role '{actualRole}' but must have role '{expectedRole}'")
		{
		}
	}
}
