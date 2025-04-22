namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.Providers;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class Cache
    {
        private readonly DataProviders dataProviders;

        private readonly ConcurrentDictionary<Guid, Resource> resourcesByDomInstanceId = new ConcurrentDictionary<Guid, Resource>();
        private readonly ConcurrentDictionary<string, Resource> resourcesByName = new ConcurrentDictionary<string, Resource>();
        private readonly ConcurrentDictionary<Guid, Resource> resourcesByCoreResourceId = new ConcurrentDictionary<Guid, Resource>();

        internal Cache(DataProviders dataProviders)
        {
            this.dataProviders = dataProviders ?? throw new ArgumentNullException(nameof(dataProviders));
        }

        public IDictionary<Guid, Resource> GetResourcesById(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var result = new Dictionary<Guid, Resource>();
            var idsToRetrieve = new List<Guid>();

            foreach (var id in ids.Where(x => x != Guid.Empty).Distinct())
            {
                if (resourcesByDomInstanceId.TryGetValue(id, out var resource))
                {
                    result[id] = resource;
                }
                else
                {
                    idsToRetrieve.Add(id);
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                var filter = new ORFilterElement<DomInstance>(idsToRetrieve.Select(id => DomInstanceExposers.Id.Equal(id)).ToArray());

                foreach (var domResource in dataProviders.ResourceStudioProvider.GetResources(filter))
                {
                    var resource = new Resource(domResource);
                    result[domResource.ID.Id] = resource;
                    resourcesByDomInstanceId[domResource.ID.Id] = resource;
                    resourcesByName[domResource.ResourceInfo.Name] = resource;

                    AddResourceIfCoreResourceAvailable(resource, domResource);
                }
            }

            return result;
        }

        public IDictionary<string, Resource> GetResourcesByName(IEnumerable<string> names)
        {
            if (names == null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var result = new Dictionary<string, Resource>();
            var namesToRetrieve = new List<string>();

            foreach (var name in names.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                if (resourcesByName.TryGetValue(name, out var resource))
                {
                    result[name] = resource;
                }
                else
                {
                    namesToRetrieve.Add(name);
                }
            }

            if (namesToRetrieve.Count > 0)
            {
                var filter = new ORFilterElement<DomInstance>(namesToRetrieve.Select(name => DomInstanceExposers.Name.Equal(name)).ToArray());
                foreach (var domResource in dataProviders.ResourceStudioProvider.GetResources(filter).Where(x => x != null))
                {
                    var resource = new Resource(domResource);
                    result[domResource.ResourceInfo.Name] = resource;
                    resourcesByDomInstanceId[domResource.ID.Id] = resource;
                    resourcesByName[domResource.ResourceInfo.Name] = resource;

                    AddResourceIfCoreResourceAvailable(resource, domResource);
                }
            }

            return result;
        }

        public IDictionary<Guid, Resource> GetResourcesByCoreResourceId(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            var result = new Dictionary<Guid, Resource>();
            var idsToRetrieve = new List<Guid>();

            foreach (var id in ids.Where(x => x != Guid.Empty).Distinct())
            {
                if (resourcesByCoreResourceId.TryGetValue(id, out var resource))
                {
                    result[id] = resource;
                }
                else
                {
                    idsToRetrieve.Add(id);
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                var filter = new ORFilterElement<DomInstance>(idsToRetrieve.Select(id => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id).Equal(id)).ToArray());
                foreach (var domResource in dataProviders.ResourceStudioProvider.GetResources(filter))
                {
                    var resource = new Resource(domResource);
                    result[domResource.ResourceInternalProperties.Resource_Id.Value] = resource;
                    resourcesByDomInstanceId[domResource.ID.Id] = resource;
                    resourcesByName[domResource.ResourceInfo.Name] = resource;
                    resourcesByCoreResourceId[domResource.ResourceInternalProperties.Resource_Id.Value] = resource;
                }
            }

            return result;
        }

        private void AddResourceIfCoreResourceAvailable(Resource resource, ResourceInstance domResource)
        {
            if (domResource.ResourceInternalProperties == null
                || !domResource.ResourceInternalProperties.Resource_Id.HasValue
                || domResource.ResourceInternalProperties.Resource_Id.Value == Guid.Empty)
            {
                return;
            }

            resourcesByCoreResourceId[domResource.ResourceInternalProperties.Resource_Id.Value] = resource;
        }
    }
}