namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="RecurringJob"/> objects.
	/// </summary>
	public interface IRecurringJobsRepository : IReadableRepository<RecurringJob>
	{
		/// <summary>
		/// Reads all Recurring Jobs.
		/// </summary>
		/// <returns>An enumerable collection of all Recurring Jobs.</returns>
		IEnumerable<RecurringJob> Read();

		/// <summary>
		/// Reads a single Recurring Job by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the Recurring Job.</param>
		/// <returns>The Recurring Job with the specified identifier, or <c>null</c> if not found.</returns>
		RecurringJob Read(Guid id);

		/// <summary>
		/// Reads multiple Recurring Jobs by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of Recurring Jobs matching the specified identifiers.</returns>
		IEnumerable<RecurringJob> Read(IEnumerable<Guid> ids);
	}
}
