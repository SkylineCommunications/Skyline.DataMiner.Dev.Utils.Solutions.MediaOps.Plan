namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a recurring job with invalid configuration.
	/// </summary>
	/// <seealso cref="RecurringJobDuplicateIdError"/>
	/// <seealso cref="RecurringJobIdInUseError"/>
	/// <seealso cref="RecurringJobInvalidDescriptionError"/>
	/// <seealso cref="RecurringJobInvalidDesiredJobStateError"/>
	/// <seealso cref="RecurringJobInvalidDurationError"/>
	/// <seealso cref="RecurringJobInvalidEndDateError"/>
	/// <seealso cref="RecurringJobInvalidNameError"/>
	/// <seealso cref="RecurringJobInvalidPatternError"/>
	/// <seealso cref="RecurringJobInvalidPostRollError"/>
	/// <seealso cref="RecurringJobInvalidPreRollError"/>
	/// <seealso cref="RecurringJobInvalidStartTimeError"/>
	/// <seealso cref="RecurringJobInvalidStateError"/>
	/// <seealso cref="RecurringJobNodeGraphError"/>
	/// <seealso cref="RecurringJobNotFoundError"/>
	/// <seealso cref="RecurringJobValueAlreadyChangedError"/>
	public class RecurringJobError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the recurring job.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
