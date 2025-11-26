namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal partial class ResourcePoolCapabilitiesSection : IConfiguredCapability
    {
        public Guid ProfileParameterId => Guid.TryParse(ProfileParameterID, out var id) ? id : Guid.Empty;
    }
}
