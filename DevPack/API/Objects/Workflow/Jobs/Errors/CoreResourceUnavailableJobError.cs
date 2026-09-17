namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents a linked core resource that is unavailable.</summary>
	public sealed class CoreResourceUnavailableJobError : JobError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J104";

		/// <summary>Initializes a new instance of the <see cref="CoreResourceUnavailableJobError"/> class.</summary>
		public CoreResourceUnavailableJobError(string resourceName, string mode)
			: base(ErrorCode, $"Resource '{resourceName}' has state {mode}")
		{
		}
	}
}