namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class DomFilterTranslator
    {
        protected DomFilterTranslator()
        {
        }

        protected static FilterElement<DomInstance> HandleGuid(Comparer comparer, object value)
        {
            return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
        }
    }
}
