namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="RecurringJob"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialQuery{T})"/>) support every
	/// field of <see cref="RecurringJobExposers"/> and return the same values as a full read. Fields that hold a value per
	/// node (<see cref="RecurringJobExposers.Resources"/> and <see cref="RecurringJobExposers.ResourcePools"/>) are read with
	/// <see cref="PartialObject{T}.GetValues{TField}(Net.Messages.SLDataGateway.DynamicListExposer{T, TField})"/>.
	/// Values are looked up by exposer instance, so read them with the exposers that were included in the query.
	/// The identifier is only returned when <see cref="RecurringJobExposers.Id"/> is included.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IRecurringJobsRepository : IRepository<RecurringJob>, IPartialPageableRepository<RecurringJob>
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
