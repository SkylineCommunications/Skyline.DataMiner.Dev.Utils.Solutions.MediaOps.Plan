namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	internal partial class ResourcepoolInstance : IIdentifiable
	{
		public Guid Id => domInstance.ID.Id;
	}
}
