namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    internal partial class ResourceInternalPropertiesSection
    {
        private ResourceMetadata resourceMetadata;

        internal IEnumerable<Guid> PoolIds
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Pool_Ids))
                {
                    return Enumerable.Empty<Guid>();
                }

                return Pool_Ids.Split([";"], StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.Parse(x));
            }
        }

        internal ResourceMetadata Metadata
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ResourceMetadata))
                {
                    resourceMetadata = new ResourceMetadata();
                }

                return resourceMetadata;
            }
        }

        internal void ApplyChanges()
        {
            if (resourceMetadata != null)
            {
                ResourceMetadata = JsonConvert.SerializeObject(resourceMetadata);
            }
        }
    }

    internal class ResourceMetadata
    {
        public string LinkedElementInfo { get; set; }

        public string LinkedServiceInfo { get; set; }

        public Guid LinkedFunctionId { get; set; }

        public string LinkedFunctionTableIndex { get; set; }
    }
}
