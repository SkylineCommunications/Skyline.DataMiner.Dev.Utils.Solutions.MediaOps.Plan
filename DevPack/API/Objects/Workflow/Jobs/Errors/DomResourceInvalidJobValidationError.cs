namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a Resource Studio resource that is incomplete or has active errors.</summary>
	public sealed class DomResourceInvalidJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J103";

		/// <summary>Initializes a new instance of the <see cref="DomResourceInvalidJobValidationError"/> class.</summary>
		public DomResourceInvalidJobValidationError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}