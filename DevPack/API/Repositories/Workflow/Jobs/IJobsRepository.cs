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
		/// Set the state of a specific orchestration event for a job.
		/// </summary>
		/// <param name="id">The unique identifier of the job.</param>
		/// <param name="updateDetails">An object containing the new state information and any associated metadata. Cannot be null.</param>
		void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails);
    }
}
