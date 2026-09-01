namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a job with invalid configuration.
	/// </summary>
	/// <seealso cref="JobCategoryNotFoundError"/>
	/// <seealso cref="JobCategoryScopeNotFoundError"/>
	/// <seealso cref="JobDuplicateIdError"/>
	/// <seealso cref="JobIdInUseError"/>
	/// <seealso cref="JobInvalidDescriptionError"/>
	/// <seealso cref="JobInvalidEndTimeError"/>
	/// <seealso cref="JobInvalidKeyError"/>
	/// <seealso cref="JobInvalidNameError"/>
	/// <seealso cref="JobInvalidNotesError"/>
	/// <seealso cref="JobInvalidPostRollError"/>
	/// <seealso cref="JobInvalidPreRollError"/>
	/// <seealso cref="JobInvalidStartTimeError"/>
	/// <seealso cref="JobInvalidStateError"/>
	/// <seealso cref="JobInvalidTimingError"/>
	/// <seealso cref="JobMandatoryConfigurationMissingError"/>
	/// <seealso cref="JobNodeGraphError"/>
	/// <seealso cref="JobNodeMandatoryConfigurationMissingError"/>
	/// <seealso cref="JobNodeResourceNotAssignedError"/>
	/// <seealso cref="JobNotFoundError"/>
	/// <seealso cref="JobPostRollEndNotReachedError"/>
	/// <seealso cref="JobPreRollStartNotReachedError"/>
	/// <seealso cref="JobRecurringJobNotFoundError"/>
	/// <seealso cref="JobReservationNotEndedError"/>
	/// <seealso cref="JobReservationNotFoundError"/>
	/// <seealso cref="JobReservationNotRunningError"/>
	/// <seealso cref="JobResourceError"/>
	/// <seealso cref="JobResourcePoolNodeNotAllowedError"/>
	/// <seealso cref="JobRunningInPostRollError"/>
	/// <seealso cref="JobRunningInPreRollError"/>
	/// <seealso cref="JobTimingChangeNotAllowedError"/>
	/// <seealso cref="JobUnresolvedReferenceError"/>
	/// <seealso cref="JobValueAlreadyChangedError"/>
	public class JobError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the job.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
