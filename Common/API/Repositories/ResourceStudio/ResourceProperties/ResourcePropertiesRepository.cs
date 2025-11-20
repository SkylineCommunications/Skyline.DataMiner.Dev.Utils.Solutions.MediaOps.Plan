namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    internal partial class ResourcePropertiesRepository : DomRepository<ResourceProperty>, IResourcePropertiesRepository
    {
        public Guid Create(ResourceProperty apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params ResourceProperty[] apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(ResourceProperty apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }
    }
}
