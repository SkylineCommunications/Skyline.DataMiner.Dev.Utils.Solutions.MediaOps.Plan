namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents the base error for a job timing value that is changed while the job state does not allow that change.
	/// The concrete type indicates which timing boundary was affected.
	/// </summary>
	/// <seealso cref="JobEndChangedNotAllowedError"/>
	/// <seealso cref="JobPostRollEndChangedNotAllowedError"/>
	/// <seealso cref="JobPreRollStartChangedNotAllowedError"/>
	/// <seealso cref="JobStartChangedNotAllowedError"/>
	public abstract class JobTimingChangedNotAllowedError : JobError
	{
		/// <summary>
		/// Gets the value that was attempted to be assigned to the timing field.
		/// </summary>
		public DateTimeOffset Value { get; internal set; }
	}
}
