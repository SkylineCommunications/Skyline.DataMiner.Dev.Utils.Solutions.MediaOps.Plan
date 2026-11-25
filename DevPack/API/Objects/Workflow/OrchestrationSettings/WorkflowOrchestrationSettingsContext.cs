namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Lazily reads the orchestration settings of a workflow, job or recurring job and of all its nodes.
	/// </summary>
	internal sealed class WorkflowOrchestrationSettingsContext : OrchestrationSettingsContext<WorkflowOrchestrationSettings, StorageWorkflow.ConfigurationInstance>
	{
		internal WorkflowOrchestrationSettingsContext(MediaOpsPlanApi planApi, ProfileParameterLookup parameters, IEnumerable<Guid?> ids)
			: base(planApi, parameters, ids)
		{
		}

		/// <summary>
		/// Creates the context of an owner (a workflow, job or recurring job) covering its own settings and those of its nodes.
		/// </summary>
		/// <param name="planApi">The API used to read the settings.</param>
		/// <param name="parameters">The profile parameter lookup shared by the read batch, or <see langword="null"/> to use a dedicated one.</param>
		/// <param name="ownerConfigurationId">The ID of the stored settings of the owner itself.</param>
		/// <param name="nodes">The stored node sections of the owner.</param>
		/// <returns>The context.</returns>
		internal static WorkflowOrchestrationSettingsContext ForOwner(MediaOpsPlanApi planApi, ProfileParameterLookup parameters, Guid? ownerConfigurationId, IEnumerable<StorageWorkflow.NodesSection> nodes)
		{
			var ids = new List<Guid?> { ownerConfigurationId };
			if (nodes != null)
			{
				ids.AddRange(nodes.Where(x => x != null).Select(x => x.NodeConfiguration));
			}

			return new WorkflowOrchestrationSettingsContext(planApi, parameters, ids);
		}

		/// <summary>
		/// Makes the specified nodes read their settings through this context.
		/// </summary>
		/// <param name="nodes">The nodes of the owner.</param>
		internal void Attach(IEnumerable<NodeBase> nodes)
		{
			foreach (var node in nodes ?? [])
			{
				node?.SetOrchestrationSettingsContext(this);
			}
		}

		private protected override IEnumerable<StorageWorkflow.ConfigurationInstance> Read(ICollection<Guid> idsToRead)
			=> PlanApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(idsToRead);

		private protected override Guid GetId(StorageWorkflow.ConfigurationInstance instance) => instance.ID.Id;

		private protected override IEnumerable<Guid> CollectProfileParameterIds(StorageWorkflow.ConfigurationInstance instance)
			=> WorkflowOrchestrationSettings.CollectProfileParameterIds(instance);

		private protected override WorkflowOrchestrationSettings Create(StorageWorkflow.ConfigurationInstance instance, ProfileParameterLookup parameters)
			=> new WorkflowOrchestrationSettings(PlanApi, instance, parameters);

		private protected override WorkflowOrchestrationSettings CreateEmpty() => new WorkflowOrchestrationSettings();
	}
}
