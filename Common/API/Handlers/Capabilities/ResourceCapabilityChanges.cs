namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

    internal class ResourceCapabilityChanges
    {
        public DomResource Resource { get; set; }

        public List<IConfiguredCapability> AddedOrUpdated { get; } = new List<IConfiguredCapability>();

        public List<Guid> Removed { get; } = new List<Guid>();
    }
}
