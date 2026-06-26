namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	internal partial class JobsInstance
	{
		private readonly DomInstanceCache domInstanceCache = new DomInstanceCache();

		private readonly ResolvedReferenceCache resolvedReferenceCache = new ResolvedReferenceCache();

		private readonly OrchestrationSettingsCache orchestrationSettingsCache = new OrchestrationSettingsCache();

		internal DomInstanceCache DomInstanceCache => domInstanceCache;

		internal ResolvedReferenceCache ResolvedReferenceCache => resolvedReferenceCache;

		internal OrchestrationSettingsCache OrchestrationSettingsCache => orchestrationSettingsCache;
	}
}
