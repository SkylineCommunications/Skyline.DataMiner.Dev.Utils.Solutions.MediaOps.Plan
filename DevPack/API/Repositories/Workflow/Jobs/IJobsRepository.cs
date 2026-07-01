namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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
		/// Marks the specified <see cref="Job"/> as completed. The job must be in the draft or tentative state and its end time must lie in the past.
		/// </summary>
		/// <param name="job">The job to mark as completed.</param>
		/// <returns>The completed job.</returns>
		Job MarkAsCompleted(Job job);

		/// <summary>
		/// Marks the specified job as completed. The job must be in the draft or tentative state and its end time must lie in the past.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to mark as completed.</param>
		/// <returns>The completed job.</returns>
		Job MarkAsCompleted(Guid jobId);

		/// <summary>
		/// Marks the specified jobs as completed. Each job must be in the draft or tentative state and its end time must lie in the past.
		/// </summary>
		/// <param name="jobs">The jobs to mark as completed.</param>
		/// <returns>A read-only collection containing the completed jobs.</returns>
		IReadOnlyCollection<Job> MarkAsCompleted(IEnumerable<Job> jobs);

		/// <summary>
		/// Marks the specified jobs as completed. Each job must be in the draft or tentative state and its end time must lie in the past.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to mark as completed.</param>
		/// <returns>A read-only collection containing the completed jobs.</returns>
		IReadOnlyCollection<Job> MarkAsCompleted(IEnumerable<Guid> jobIds);

		/// <summary>
		/// Set the state of a specific orchestration event for a job.
		/// </summary>
		/// <param name="id">The unique identifier of the job.</param>
		/// <param name="updateDetails">An object containing the new state information and any associated metadata. Cannot be null.</param>
		void SetOrchestrationState(Guid id, OrchestrationUpdateDetails updateDetails);

		/// <summary>
		/// Deletes the specified <see cref="Job"/> using the provided <see cref="JobDeleteOptions"/>.
		/// </summary>
		/// <param name="job">The job to delete.</param>
		/// <param name="options">Options specifying how the job should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> or <paramref name="options"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified job.</exception>
		void Delete(Job job, JobDeleteOptions options);

		/// <summary>
		/// Deletes the job with the specified identifier using the provided <see cref="JobDeleteOptions"/>.
		/// </summary>
		/// <param name="jobId">The unique identifier of the job to delete.</param>
		/// <param name="options">Options specifying how the job should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified job.</exception>
		void Delete(Guid jobId, JobDeleteOptions options);

		/// <summary>
		/// Deletes the specified jobs using the provided <see cref="JobDeleteOptions"/>.
		/// </summary>
		/// <param name="jobs">The jobs to delete.</param>
		/// <param name="options">Options specifying how the jobs should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="jobs"/> or <paramref name="options"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more jobs.</exception>
		void Delete(IEnumerable<Job> jobs, JobDeleteOptions options);

		/// <summary>
		/// Deletes the jobs with the specified identifiers using the provided <see cref="JobDeleteOptions"/>.
		/// </summary>
		/// <param name="jobIds">The unique identifiers of the jobs to delete.</param>
		/// <param name="options">Options specifying how the jobs should be deleted.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="jobIds"/> or <paramref name="options"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more jobs.</exception>
		void Delete(IEnumerable<Guid> jobIds, JobDeleteOptions options);
    }
}
