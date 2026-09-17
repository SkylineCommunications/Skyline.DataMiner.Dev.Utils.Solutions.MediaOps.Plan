namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents job references that could not be resolved.</summary>
	public sealed class UnresolvedReferencesJobError : JobError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J601";

		/// <summary>Initializes a new instance of the <see cref="UnresolvedReferencesJobError"/> class.</summary>
		public UnresolvedReferencesJobError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}