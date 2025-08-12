namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.API;

    internal partial class ResourceCapabilitiesSection : IConfiguredCapability
    {
        public Guid ProfileParameterId => Guid.TryParse(ProfileParameterID, out var id) ? id : Guid.Empty;
    }
}
