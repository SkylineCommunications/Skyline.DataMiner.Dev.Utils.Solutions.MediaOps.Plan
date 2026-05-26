namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Defines methods for managing <see cref="Workflow"/> objects.
	/// </summary>
	public interface IWorkflowsRepository : IRepository<Workflow>
	{
		/// <summary>
		/// Moves the specified <see cref="Workflow"/> from draft to complete state.
		/// </summary>
		/// <param name="workflow">The workflow to move.</param>
		/// <returns>The completed workflow.</returns>
		Workflow Complete(Workflow workflow);

		/// <summary>
		/// Moves the specified workflow from draft to complete state.
		/// </summary>
		/// <param name="workflowId">The unique identifier of the workflow to move.</param>
		/// <returns>The completed workflow.</returns>
		Workflow Complete(Guid workflowId);

		/// <summary>
		/// Moves the specified workflows from draft to complete state.
		/// </summary>
		/// <param name="workflows">The workflows to move.</param>
		/// <returns>A read-only collection containing the completed workflows.</returns>
		IReadOnlyCollection<Workflow> Complete(IEnumerable<Workflow> workflows);

		/// <summary>
		/// Moves the specified workflows from draft to complete state.
		/// </summary>
		/// <param name="workflowIds">The unique identifiers of the workflows.</param>
		/// <returns>A read-only collection containing the completed workflows.</returns>
		IReadOnlyCollection<Workflow> Complete(IEnumerable<Guid> workflowIds);
	}
}
