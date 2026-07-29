namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Defines methods for managing <see cref="RecurringJob"/> objects.
	/// </summary>
	public interface IRecurringJobsRepository : IRepository<RecurringJob>
	{
		/// <summary>
		/// Moves the specified <see cref="RecurringJob"/> from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Completed"/> state.
		/// </summary>
		/// <param name="recurringJob">The recurring job to complete.</param>
		/// <returns>The completed recurring job.</returns>
		RecurringJob Complete(RecurringJob recurringJob);

		/// <summary>
		/// Moves the specified <see cref="RecurringJob"/> from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Completed"/> state.
		/// </summary>
		/// <param name="recurringJobId">The unique identifier of the recurring job to complete.</param>
		/// <returns>The completed recurring job.</returns>
		RecurringJob Complete(Guid recurringJobId);

		/// <summary>
		/// Moves the specified Recurring Jobs from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Completed"/> state.
		/// </summary>
		/// <param name="recurringJobs">The recurring jobs to complete.</param>
		/// <returns>A read-only collection containing the completed recurring jobs.</returns>
		IReadOnlyCollection<RecurringJob> Complete(IEnumerable<RecurringJob> recurringJobs);

		/// <summary>
		/// Moves the specified Recurring Jobs from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Completed"/> state.
		/// </summary>
		/// <param name="recurringJobIds">The unique identifiers of the recurring jobs to complete.</param>
		/// <returns>A read-only collection containing the completed recurring jobs.</returns>
		IReadOnlyCollection<RecurringJob> Complete(IEnumerable<Guid> recurringJobIds);

		/// <summary>
		/// Moves the specified <see cref="RecurringJob"/> from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Cancelled"/> state.
		/// </summary>
		/// <param name="recurringJob">The recurring job to cancel.</param>
		/// <returns>The canceled recurring job.</returns>
		RecurringJob Cancel(RecurringJob recurringJob);

		/// <summary>
		/// Moves the specified <see cref="RecurringJob"/> from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Cancelled"/> state.
		/// </summary>
		/// <param name="recurringJobId">The unique identifier of the recurring job to cancel.</param>
		/// <returns>The canceled recurring job.</returns>
		RecurringJob Cancel(Guid recurringJobId);

		/// <summary>
		/// Moves the specified Recurring Jobs from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Cancelled"/> state.
		/// </summary>
		/// <param name="recurringJobs">The recurring jobs to cancel.</param>
		/// <returns>A read-only collection containing the canceled recurring jobs.</returns>
		IReadOnlyCollection<RecurringJob> Cancel(IEnumerable<RecurringJob> recurringJobs);

		/// <summary>
		/// Moves the specified Recurring Jobs from <see cref="RecurringJobState.Active"/> to <see cref="RecurringJobState.Cancelled"/> state.
		/// </summary>
		/// <param name="recurringJobIds">The unique identifiers of the recurring jobs to cancel.</param>
		/// <returns>A read-only collection containing the canceled recurring jobs.</returns>
		IReadOnlyCollection<RecurringJob> Cancel(IEnumerable<Guid> recurringJobIds);

		/// <summary>
		/// Updates the <see cref="RecurringJob.ProcessState"/> of the recurring job with the specified ID.
		/// </summary>
		/// <param name="recurringJobId">The unique identifier of the recurring job to update.</param>
		/// <param name="processState">The new process state to set.</param>
		/// <returns>The updated recurring job, or <see langword="null"/> if no recurring job with the given ID exists.</returns>
		RecurringJob UpdateProcessState(Guid recurringJobId, RecurringJobProcessState processState);

		/// <summary>
		/// Updates the <see cref="RecurringJob.ProcessState"/> of the specified recurring job.
		/// </summary>
		/// <param name="recurringJob">The recurring job to update.</param>
		/// <param name="processState">The new process state to set.</param>
		/// <returns>The updated recurring job.</returns>
		RecurringJob UpdateProcessState(RecurringJob recurringJob, RecurringJobProcessState processState);
	}
}
