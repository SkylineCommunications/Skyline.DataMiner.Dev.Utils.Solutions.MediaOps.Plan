namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>Represents an unexpected job validation failure.</summary>
	public sealed class GenericJobValidatorError : JobValidatorError
	{
		/// <summary>The error code.</summary>
		public const string ErrorCode = "J000";

		/// <summary>Initializes a new instance of the <see cref="GenericJobValidatorError"/> class.</summary>
		public GenericJobValidatorError(Exception exception)
			: base(ErrorCode, $"ERROR: {exception ?? throw new ArgumentNullException(nameof(exception))}")
		{
		}
	}
}