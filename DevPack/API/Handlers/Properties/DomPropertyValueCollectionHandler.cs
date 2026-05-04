namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal static class DomPropertyValueCollectionHandler
	{
		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiObjects, out DomInstanceBulkOperationResult<PropertyValuesInstance> result)
		{
			throw new NotImplementedException();
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiObjects, out DomInstanceBulkOperationResult<PropertyValuesInstance> result)
		{
			throw new NotImplementedException();
		}
	}
}
