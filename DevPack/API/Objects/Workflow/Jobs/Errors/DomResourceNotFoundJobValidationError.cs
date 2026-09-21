namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>Represents a Resource Studio resource referenced by a job node that could not be found.</summary>
	public sealed class DomResourceNotFoundJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J101";

		/// <summary>Initializes a new instance of the <see cref="DomResourceNotFoundJobValidationError"/> class.</summary>
		public DomResourceNotFoundJobValidationError(Guid resourceId, string nodeId)
			: base(ErrorCode, $"Couldn't find DOM resource with ID '{resourceId}' for node {nodeId}")
		{
		}
	}
}