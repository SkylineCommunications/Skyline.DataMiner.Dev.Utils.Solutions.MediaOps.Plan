namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a linked core resource that is unavailable.</summary>
	public class CoreResourceUnavailableJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J104";

		/// <summary>Initializes a new instance of the <see cref="CoreResourceUnavailableJobValidationError"/> class.</summary>
		public CoreResourceUnavailableJobValidationError(string resourceName, string mode)
			: base(ErrorCode, $"Resource '{resourceName}' has state {mode}")
		{
		}
	}
}