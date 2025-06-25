namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ResourcePoolsRepository : RepositoryBase<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(IMediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public Guid Create(ResourcePool apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            if (apiObject.Id != Guid.Empty)
            {
                // Check if the object already exists
            }

            // Validate name

            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params ResourcePool[] objectApis)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] objectIds)
        {
            throw new NotImplementedException();
        }

        public ResourcePool Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPage()
        {
            throw new NotImplementedException();
        }

        public Guid Update(ResourcePool apiObject)
        {
            throw new NotImplementedException();
        }
    }
}
