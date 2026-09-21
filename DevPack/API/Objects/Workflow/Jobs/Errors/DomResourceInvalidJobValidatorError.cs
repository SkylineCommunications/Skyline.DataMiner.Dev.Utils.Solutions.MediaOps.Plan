namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a Resource Studio resource that is incomplete or has active errors.</summary>
	public sealed class DomResourceInvalidJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J103";

		/// <summary>Initializes a new instance of the <see cref="DomResourceInvalidJobValidatorError"/> class.</summary>
		public DomResourceInvalidJobValidatorError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}