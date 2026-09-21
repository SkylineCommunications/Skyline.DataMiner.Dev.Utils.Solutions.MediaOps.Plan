namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a validator-owned error that is reported on a <see cref="Job"/>.
	/// </summary>
	public class JobValidationError : JobError
	{
		/// <summary>
		/// Gets a set of error codes that are managed by the MediaOps Plan solution.
		/// </summary>
		public static readonly HashSet<string> MediaOpsOwnedErrorCodes = new HashSet<string>(StringComparer.Ordinal)
		{
			GenericJobValidationError.ErrorCode,
			DomResourceNotFoundJobValidationError.ErrorCode,
			CoreResourceNotFoundJobValidationError.ErrorCode,
			DomResourceInvalidJobValidationError.ErrorCode,
			CoreResourceUnavailableJobValidationError.ErrorCode,
			QuarantinedReservationJobValidationError.ErrorCode,
			VirtualSignalGroupNotFoundJobValidationError.ErrorCode,
			TransitionToTentativeJobValidationError.ErrorCode,
			UnresolvedReferencesJobValidationError.ErrorCode,
			ReservationRequirementsMismatchJobValidationError.ErrorCode,
			LiveEventsMismatchJobValidationError.ErrorCode,
		};

		/// <summary>
		/// Initializes a new instance of the <see cref="JobValidationError"/> class.
		/// </summary>
		/// <param name="code">The code that identifies the error.</param>
		/// <param name="message">The message that describes the error.</param>
		public JobValidationError(string code, string message)
			: base(code, message)
		{
		}
	}
}
