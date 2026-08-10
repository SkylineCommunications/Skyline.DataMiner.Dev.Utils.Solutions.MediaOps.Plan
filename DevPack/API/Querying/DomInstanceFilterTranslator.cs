namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	using Comparer = Net.Messages.SLDataGateway.Comparer;

	internal abstract class DomInstanceFilterTranslator<T> : FilterTranslator<T, DomInstance> where T : ApiObject
	{
		protected DomInstanceFilterTranslator()
		{
		}

		protected abstract FilterElement<DomInstance> DomDefinitionFilter { get; }

		protected static FilterElement<DomInstance> HandleGuid(Comparer comparer, object value)
		{
			return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
		}

		protected static IOrderByElement HandleGuid(SortOrder sortOrder, bool naturalSort)
		{
			return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
		}

		public override FilterElement<DomInstance> TranslateFilter(FilterElement<T> filter)
		{
			return base.TranslateFilter(filter).AND(DomDefinitionFilter);
		}
	}
}
