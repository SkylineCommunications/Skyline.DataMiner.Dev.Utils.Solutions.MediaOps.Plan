namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.Plan.API.Validators;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ResourcePoolsRepository : RepositoryBase<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
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

            ValidateName(apiObject.Name, apiObject.State);


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

        public void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            MoveTo(resourcePool.Id, desiredState);
        }

        public void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

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

        private void ValidateName(string name, ResourcePoolState resourcePoolState)
        {
            if (!InputValidator.ValidateEmptyText(name))
            {
                // todo: throw new exception
                return;
            }

            if (!InputValidator.ValidateTextLength(name))
            {
                // todo: throw new exception
                return;
            }

            if (resourcePoolState != ResourcePoolState.Complete)
            {
                return;
            }

            // todo: check if name is unique in DOM

            // todo: check if name is unique in core

        }
    }
}
