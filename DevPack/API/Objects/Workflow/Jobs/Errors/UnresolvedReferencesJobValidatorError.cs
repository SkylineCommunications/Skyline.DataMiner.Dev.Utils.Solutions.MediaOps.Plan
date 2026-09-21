namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents job references that could not be resolved.</summary>
	public sealed class UnresolvedReferencesJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J601";

		/// <summary>Initializes a new instance of the <see cref="UnresolvedReferencesJobValidatorError"/> class.</summary>
		public UnresolvedReferencesJobValidatorError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}