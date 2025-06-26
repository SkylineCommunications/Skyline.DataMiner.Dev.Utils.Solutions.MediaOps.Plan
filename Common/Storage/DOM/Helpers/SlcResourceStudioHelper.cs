namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class SlcResourceStudioHelper : DomModuleHelperBase
    {
        public SlcResourceStudioHelper(IConnection connection) : base(SlcResource_Studio.SlcResource_StudioIds.ModuleId, connection)
        {
        }

        public long CountResourceStudioInstances(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return DomHelper.DomInstances.Count(filter);
        }

        public IEnumerable<ResourcepoolInstance> GetResourcePools(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourcePoolIterator(filter);
        }

        private IEnumerable<ResourcepoolInstance> GetResourcePoolIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourcepoolInstance(instance));
        }
    }
}
