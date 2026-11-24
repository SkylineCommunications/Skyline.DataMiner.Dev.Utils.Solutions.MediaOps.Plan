namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Lazily reads the orchestration settings of a resource pool.
	/// </summary>
	internal sealed class ResourceStudioOrchestrationSettingsContext : OrchestrationSettingsContext<ResourceStudioOrchestrationSettings, StorageResourceStudio.ConfigurationInstance>
	{
		internal ResourceStudioOrchestrationSettingsContext(MediaOpsPlanApi planApi, ProfileParameterLookup parameters, IEnumerable<Guid?> ids)
			: base(planApi, parameters, ids)
		{
		}

		private protected override IEnumerable<StorageResourceStudio.ConfigurationInstance> Read(ICollection<Guid> idsToRead)
			=> PlanApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(idsToRead);

		private protected override Guid GetId(StorageResourceStudio.ConfigurationInstance instance) => instance.ID.Id;

		private protected override IEnumerable<Guid> CollectProfileParameterIds(StorageResourceStudio.ConfigurationInstance instance)
			=> ResourceStudioOrchestrationSettings.CollectProfileParameterIds(instance);

		private protected override ResourceStudioOrchestrationSettings Create(StorageResourceStudio.ConfigurationInstance instance, ProfileParameterLookup parameters)
			=> new ResourceStudioOrchestrationSettings(PlanApi, instance, parameters);

		private protected override ResourceStudioOrchestrationSettings CreateEmpty() => new ResourceStudioOrchestrationSettings();
	}
}
