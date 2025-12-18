namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal abstract class DomInstanceFilterTranslator<T> : FilterTranslator<T, DomInstance> where T : ApiObject
    {
        protected DomInstanceFilterTranslator()
        {
        }

        protected static FilterElement<DomInstance> HandleGuid(Comparer comparer, object value)
        {
            return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
        }

        protected abstract FilterElement<DomInstance> DomDefinitionFilter { get; }

        public override FilterElement<DomInstance> Translate(FilterElement<T> filter)
        {
            return base.Translate(filter).AND(DomDefinitionFilter);
        }
    }
}
