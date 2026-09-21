namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>Represents job references that could not be resolved.</summary>
	public class UnresolvedReferencesJobValidationError : JobValidationError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J601";

		/// <summary>Initializes a new instance of the <see cref="UnresolvedReferencesJobValidationError"/> class.</summary>
		public UnresolvedReferencesJobValidationError(string message)
			: base(ErrorCode, message)
		{
		}
	}
}