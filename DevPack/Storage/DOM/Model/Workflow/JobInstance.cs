namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	internal partial class JobsInstance
	{
		private readonly DomInstanceCache domInstanceCache = new DomInstanceCache();

		private readonly ResolvedReferenceCache resolvedReferenceCache = new ResolvedReferenceCache();

		internal DomInstanceCache DomInstanceCache => domInstanceCache;

		internal ResolvedReferenceCache ResolvedReferenceCache => resolvedReferenceCache;
	}
}
