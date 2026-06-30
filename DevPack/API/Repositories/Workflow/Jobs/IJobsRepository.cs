namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Defines methods for managing <see cref="Job"/> objects, including state transitions.
	/// </summary>
	public interface IJobsRepository : IRepository<Job>
    {
		/// <summary>
		/// Gets the identifiers for predefined job types.
		/// </summary>
		public JobTypes JobTypes { get; }

		/// <summary>
		/// Moves the specified <see cref="Job"/> from draft to tentative state.
		/// </summary>
		/// <param name="job">The job to move.</param>
		/// <returns>The tentative job.</returns>
		Job SaveAsTentative(Job job);

		/// <summary>
		/// Moves the specified job from draft to tentative state.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to move.</param>
		/// <returns>The tentative job.</returns>
		Job SaveAsTentative(Guid jobId);

		/// <summary>
		/// Moves the specified jobs from draft to tentative state.
		/// </summary>
		/// <param name="jobs">The jobs to move.</param>
		/// <returns>A read-only collection containing the tentative jobs.</returns>
		IReadOnlyCollection<Job> SaveAsTentative(IEnumerable<Job> jobs);

		/// <summary>
		/// Moves the specified jobs from draft to tentative state.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to move.</param>
		/// <returns>A read-only collection containing the tentative jobs.</returns>
		IReadOnlyCollection<Job> SaveAsTentative(IEnumerable<Guid> jobIds);

		/// <summary>
		/// Moves the specified <see cref="Job"/> from tentative to confirmed state.
		/// </summary>
		/// <param name="job">The job to confirm.</param>
		/// <returns>The confirmed job.</returns>
		Job Confirm(Job job);

		/// <summary>
		/// Moves the specified job from tentative to confirmed state.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to confirm.</param>
		/// <returns>The confirmed job.</returns>
		Job Confirm(Guid jobId);

		/// <summary>
		/// Moves the specified jobs from tentative to confirmed state.
		/// </summary>
		/// <param name="jobs">The jobs to confirm.</param>
		/// <returns>A read-only collection containing the confirmed jobs.</returns>
		IReadOnlyCollection<Job> Confirm(IEnumerable<Job> jobs);

		/// <summary>
		/// Moves the specified jobs from tentative to confirmed state.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to confirm.</param>
		/// <returns>A read-only collection containing the confirmed jobs.</returns>
		IReadOnlyCollection<Job> Confirm(IEnumerable<Guid> jobIds);

		/// <summary>
		/// Cancels the specified <see cref="Job"/>, which must be in the tentative or confirmed state.
		/// </summary>
		/// <param name="job">The job to cancel.</param>
		/// <returns>The canceled job.</returns>
		Job Cancel(Job job);

		/// <summary>
		/// Cancels the specified job, which must be in the tentative or confirmed state.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to cancel.</param>
		/// <returns>The canceled job.</returns>
		Job Cancel(Guid jobId);

		/// <summary>
		/// Cancels the specified jobs, which must be in the tentative or confirmed state.
		/// </summary>
		/// <param name="jobs">The jobs to cancel.</param>
		/// <returns>A read-only collection containing the canceled jobs.</returns>
		IReadOnlyCollection<Job> Cancel(IEnumerable<Job> jobs);

		/// <summary>
		/// Cancels the specified jobs, which must be in the tentative or confirmed state.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to cancel.</param>
		/// <returns>A read-only collection containing the canceled jobs.</returns>
		IReadOnlyCollection<Job> Cancel(IEnumerable<Guid> jobIds);

		/// <summary>
		/// Returns the specified <see cref="Job"/> from confirmed back to tentative state.
		/// </summary>
		/// <param name="job">The job to return to tentative state.</param>
		/// <returns>The job that was returned to tentative state.</returns>
		Job ReturnToTentative(Job job);

		/// <summary>
		/// Returns the specified job from confirmed back to tentative state.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to return to tentative state.</param>
		/// <returns>The job that was returned to tentative state.</returns>
		Job ReturnToTentative(Guid jobId);

		/// <summary>
		/// Returns the specified jobs from confirmed back to tentative state.
		/// </summary>
		/// <param name="jobs">The jobs to return to tentative state.</param>
		/// <returns>A read-only collection containing the jobs that were returned to tentative state.</returns>
		IReadOnlyCollection<Job> ReturnToTentative(IEnumerable<Job> jobs);

		/// <summary>
		/// Returns the specified jobs from confirmed back to tentative state.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to return to tentative state.</param>
		/// <returns>A read-only collection containing the jobs that were returned to tentative state.</returns>
		IReadOnlyCollection<Job> ReturnToTentative(IEnumerable<Guid> jobIds);

		/// <summary>
		/// Set the state of a specific orchestration event for a job.
		/// </summary>
		/// <param name="id">The unique identifier of the job.</param>
		/// <param name="updateDetails">An object containing the new state information and any associated metadata. Cannot be null.</param>
		void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails);
    }
}
