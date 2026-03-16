namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

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

		public override FilterElement<DomInstance> Translate(FilterElement<T> filter)
		{
			return base.Translate(filter).AND(DomDefinitionFilter);
		}
	}
}
