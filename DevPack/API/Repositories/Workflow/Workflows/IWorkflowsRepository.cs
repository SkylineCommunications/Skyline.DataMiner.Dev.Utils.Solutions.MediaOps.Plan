namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.SDM;

	/// <summary>
	/// Defines methods for managing <see cref="Workflow"/> objects.
	/// </summary>
	/// <remarks>
	/// Partial reads (<see cref="IPartialReadableRepository{T}.ReadPartial(IPartialFilterElement{T})"/>) support every
	/// field of <see cref="WorkflowExposers"/> and return the same values as a full read. Fields that hold a value per node
	/// (<see cref="WorkflowExposers.Resources"/> and <see cref="WorkflowExposers.ResourcePools"/>) are read with
	/// <see cref="PartialObject{T}.GetValues{TField}(Net.Messages.SLDataGateway.DynamicListExposer{T, TField})"/>.
	/// <see cref="IPartialPageableRepository{T}.ReadPartialPaged(IPartialQuery{T})"/> returns the same objects in pages of 200.
	/// </remarks>
	public interface IWorkflowsRepository : IRepository<Workflow>, IPartialPageableRepository<Workflow>
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
