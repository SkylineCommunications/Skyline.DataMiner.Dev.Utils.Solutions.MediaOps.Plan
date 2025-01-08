namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    using DomHelpers.SlcResource_Studio;

    internal class Resource : IResource
    {
        private ResourceInstance domResource;

        internal Resource (ResourceInstance domResource)
        {
            this.domResource = domResource ?? throw new ArgumentNullException(nameof(domResource));

            ParseDomResource();
        }

        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public long Concurrency { get; private set; }

        public ResourceStatus Status { get; private set; }

        private void ParseDomResource()
        {
            Id = domResource.ID.Id;
            Name = domResource.ResourceInfo.Name;
            Concurrency = domResource.ResourceInfo.Concurrency.HasValue ? domResource.ResourceInfo.Concurrency.Value : 1;
            Status = EnumConvert.ConvertResourceStatus(domResource.StatusId);
        }
    }
}
