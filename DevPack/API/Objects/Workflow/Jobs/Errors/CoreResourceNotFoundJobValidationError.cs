namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>Represents a linked core resource that could not be found.</summary>
	public sealed class CoreResourceNotFoundJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J102";

		/// <summary>Initializes a new instance of the <see cref="CoreResourceNotFoundJobValidationError"/> class.</summary>
		public CoreResourceNotFoundJobValidationError(Guid coreResourceId, string resourceName)
			: base(ErrorCode, $"Couldn't find core resource with ID '{coreResourceId}' linked to DOM resource '{resourceName}'")
		{
		}
	}
}