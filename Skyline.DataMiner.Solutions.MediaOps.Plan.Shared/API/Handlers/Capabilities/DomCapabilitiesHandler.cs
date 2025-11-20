namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

    // Todo: Take code from MediaOps solution
    internal class DomCapabilitiesHandler
    {
        #region Fields
        private readonly MediaOpsPlanApi planApi;

        private readonly Dictionary<Guid, DomResourcePool> domResourcePoolsById = new Dictionary<Guid, DomResourcePool>();

        private readonly Dictionary<Guid, IReadOnlyCollection<IConfiguredCapability>> capabilitiesByDomResourceId = new Dictionary<Guid, IReadOnlyCollection<IConfiguredCapability>>();

        private readonly Dictionary<Guid, IReadOnlyCollection<IConfiguredCapability>> capabilitiesByDomResourcePoolId = new Dictionary<Guid, IReadOnlyCollection<IConfiguredCapability>>();
        #endregion

        public DomCapabilitiesHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        #region Methods
        public IReadOnlyCollection<ResourceCapabilityChanges> GetCoreResourceCapabilitiesChanges(DomResourcePool previous, DomResourcePool current)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (previous.ID.Id != current.ID.Id)
            {
                throw new NotSupportedException("It is not allowed to compare two different resource pools.");
            }

            var previousCapabilitiesById = previous.ResourcePoolCapabilities.ToDictionary(x => Guid.Parse(x.ProfileParameterID));
            var currentCapabilitiesById = current.ResourcePoolCapabilities.ToDictionary(x => Guid.Parse(x.ProfileParameterID));

            var added = currentCapabilitiesById.Keys.Except(previousCapabilitiesById.Keys).ToList();
            var removed = previousCapabilitiesById.Keys.Except(currentCapabilitiesById.Keys).ToList();

            var equalOrUpdated = currentCapabilitiesById.Keys.Except(added);
            var updated = equalOrUpdated.Where(x => !previousCapabilitiesById[x].StringValue.Split(';').SequenceEqual(currentCapabilitiesById[x].StringValue.Split(';'))).ToList();

            if (added.Count == 0 && removed.Count == 0 && updated.Count == 0)
            {
                return new List<ResourceCapabilityChanges>();
            }

            var addedOrUpdated = new List<Guid>(added);
            addedOrUpdated.AddRange(updated);

            var requiredCapabilitiesByDomResource = GetAllResourcesCapabilitiesFromPool(current, new List<Guid> { current.ID.Id });
            var configuredPoolCapabilities = GetPoolCapabilitiesByPoolIds(new List<Guid> { current.ID.Id })?.Values.FirstOrDefault();

            var allChanges = new List<ResourceCapabilityChanges>();
            foreach (var kvp in requiredCapabilitiesByDomResource)
            {
                var resourceChanges = ComposeResourceCapabilityChanges(kvp.Key, kvp.Value, configuredPoolCapabilities, addedOrUpdated, removed);

                allChanges.Add(resourceChanges);
            }

            return allChanges;
        }

        public ResourceCapabilityChanges GetCoreResourceCapabilitiesChanges(DomResource previous, DomResource current)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            if (previous.ID.Id != current.ID.Id)
            {
                throw new NotSupportedException("It is not allowed to compare two different resources.");
            }

            var previousCapabilitiesById = previous.ResourceCapabilities.ToDictionary(x => Guid.Parse(x.ProfileParameterID));
            var currentCapabilitiesById = current.ResourceCapabilities.ToDictionary(x => Guid.Parse(x.ProfileParameterID));

            var added = currentCapabilitiesById.Keys.Except(previousCapabilitiesById.Keys).ToList();
            var removed = previousCapabilitiesById.Keys.Except(currentCapabilitiesById.Keys).ToList();

            var equalOrUpdated = currentCapabilitiesById.Keys.Except(added);
            var updated = equalOrUpdated.Where(x => !previousCapabilitiesById[x].StringValue.Split(';').SequenceEqual(currentCapabilitiesById[x].StringValue.Split(';'))).ToList();

            if (added.Count == 0 && removed.Count == 0 && updated.Count == 0)
            {
                return null;
            }

            var addedOrUpdated = new List<Guid>(added);
            addedOrUpdated.AddRange(updated);

            var poolCapabilitiesByPoolIds = GetPoolCapabilitiesByPoolIds(current.ResourceInternalProperties.PoolIds);
            var capabilitiesToMerge = new List<IConfiguredCapability>();
            foreach (var poolCapabilities in poolCapabilitiesByPoolIds.Values)
            {
                capabilitiesToMerge.AddRange(poolCapabilities);
            }

            var requiredCapabilities = MergeCapabilities(capabilitiesToMerge);
            var configuredResourceCapabilities = GetResourceCapabilities(current);

            var changes = ComposeResourceCapabilityChanges(current, requiredCapabilities, configuredResourceCapabilities, addedOrUpdated, removed);

            return changes;
        }

        public IReadOnlyCollection<IConfiguredCapability> GetExpectedCoreResourceCapabilities(DomResource domResource)
        {
            if (domResource == null)
            {
                throw new ArgumentNullException(nameof(domResource));
            }

            var poolCapabilitiesByPoolIds = GetPoolCapabilitiesByPoolIds(domResource.ResourceInternalProperties.PoolIds);
            var resourceCapabilities = GetResourceCapabilities(domResource);

            var capabilitiesToMerge = new List<IConfiguredCapability>(resourceCapabilities);
            foreach (var poolCapabilities in poolCapabilitiesByPoolIds.Values)
            {
                capabilitiesToMerge.AddRange(poolCapabilities);
            }

            return MergeCapabilities(capabilitiesToMerge);
        }

        public IReadOnlyDictionary<DomResource, IReadOnlyCollection<IConfiguredCapability>> GetExpectedCoreResourceCapabilities(DomResourcePool domResourcePool)
        {
            if (domResourcePool == null)
            {
                throw new ArgumentNullException(nameof(domResourcePool));
            }

            return GetAllResourcesCapabilitiesFromPool(domResourcePool);
        }

        public IReadOnlyCollection<IConfiguredCapability> GetConfiguredCapabilities(DomResourcePool domResourcePool)
        {
            var poolCapabilitiesByPoolIds = GetPoolCapabilitiesByPoolIds(new List<Guid> { domResourcePool.ID.Id });

            return poolCapabilitiesByPoolIds[domResourcePool.ID.Id];
        }

        private static IReadOnlyCollection<IConfiguredCapability> MergeCapabilities(IEnumerable<IConfiguredCapability> capabilities)
        {
            var configuredCapabilitiesById = new Dictionary<Guid, ConfiguredCapability>();
            if (capabilities == null)
            {
                return configuredCapabilitiesById.Values;
            }

            foreach (var capability in capabilities)
            {
                if (string.IsNullOrEmpty(capability.StringValue))
                {
                    continue;
                }

                if (!configuredCapabilitiesById.TryGetValue(capability.ProfileParameterId, out var configuredCapability))
                {
                    configuredCapability = new ConfiguredCapability(capability.ProfileParameterId);

                    configuredCapabilitiesById.Add(capability.ProfileParameterId, configuredCapability);
                }

                var discretes = configuredCapability.StringValue.Split(';').ToList();
                discretes.AddRange(capability.StringValue.Split(';'));

                configuredCapability.StringValue = string.Join(";", discretes.Distinct());
            }

            return configuredCapabilitiesById.Values;
        }

        private ResourceCapabilityChanges ComposeResourceCapabilityChanges(DomResource resource, IEnumerable<IConfiguredCapability> requiredCapabilities, IEnumerable<IConfiguredCapability> changedObjectCapabilities, IEnumerable<Guid> addedOrUpdated, IEnumerable<Guid> removed)
        {
            // changedObjectCapabilities represents the current capabilities on the changed resource or changed resource pool
            var changes = new ResourceCapabilityChanges
            {
                Resource = resource,
            };

            var requiredCapabilitiesById = requiredCapabilities?.ToDictionary(x => x.ProfileParameterId) ?? new Dictionary<Guid, IConfiguredCapability>();
            var changedObjectCapabilitiesById = changedObjectCapabilities?.ToDictionary(x => x.ProfileParameterId) ?? new Dictionary<Guid, IConfiguredCapability>();

            foreach (var capabilityId in addedOrUpdated)
            {
                if (!changedObjectCapabilitiesById.TryGetValue(capabilityId, out var changedObjectCapability))
                {
                    continue;
                }

                if (!requiredCapabilitiesById.TryGetValue(capabilityId, out var requiredCapability))
                {
                    changes.AddedOrUpdated.Add(changedObjectCapability);

                    continue;
                }

                var capability = new ConfiguredCapability(requiredCapability.ProfileParameterId);

                var discretes = new List<string>(requiredCapability.StringValue.Split(';'));
                discretes.AddRange(changedObjectCapability.StringValue.Split(';'));
                capability.StringValue = string.Join(";", discretes.Distinct());

                changes.AddedOrUpdated.Add(capability);
            }

            foreach (var capabilityId in removed)
            {
                if (!requiredCapabilitiesById.TryGetValue(capabilityId, out var requiredCapability))
                {
                    changes.Removed.Add(capabilityId);

                    continue;
                }

                changes.AddedOrUpdated.Add(requiredCapability);
            }

            return changes;
        }

        private IReadOnlyDictionary<DomResource, IReadOnlyCollection<IConfiguredCapability>> GetAllResourcesCapabilitiesFromPool(DomResourcePool domResourcePool, IEnumerable<Guid> excludedPools = null)
        {
            var capabilitiesByDomResource = new Dictionary<DomResource, IReadOnlyCollection<IConfiguredCapability>>();

            foreach (var domResource in planApi.DomHelpers.SlcResourceStudioHelper.GetResourcesByPool(domResourcePool))
            {
                var poolIds = (excludedPools == null) ? domResource.ResourceInternalProperties.PoolIds : domResource.ResourceInternalProperties.PoolIds.Except(excludedPools);
                var poolCapabilitiesByPoolIds = GetPoolCapabilitiesByPoolIds(poolIds);
                var resourceCapabilities = GetResourceCapabilities(domResource);

                var capabilitiesToMerge = new List<IConfiguredCapability>(resourceCapabilities);
                foreach (var poolCapabilities in poolCapabilitiesByPoolIds.Values)
                {
                    capabilitiesToMerge.AddRange(poolCapabilities);
                }

                var mergedCapabilities = MergeCapabilities(capabilitiesToMerge);
                capabilitiesByDomResource.Add(domResource, mergedCapabilities);
            }

            return capabilitiesByDomResource;
        }

        private IReadOnlyCollection<IConfiguredCapability> GetResourceCapabilities(DomResource domResource)
        {
            if (capabilitiesByDomResourceId.TryGetValue(domResource.ID.Id, out var resourceCapabilities))
            {
                return resourceCapabilities;
            }

            resourceCapabilities = domResource.ResourceCapabilities.OfType<IConfiguredCapability>().ToList();
            capabilitiesByDomResourceId[domResource.ID.Id] = resourceCapabilities;

            return resourceCapabilities;
        }

        private IDictionary<Guid, IReadOnlyCollection<IConfiguredCapability>> GetPoolCapabilitiesByPoolIds(IEnumerable<Guid> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException(nameof(poolIds));
            }

            var result = new Dictionary<Guid, IReadOnlyCollection<IConfiguredCapability>>();
            var idsToRetrieve = new List<Guid>();

            foreach (var id in poolIds.Where(x => x != Guid.Empty).Distinct())
            {
                if (capabilitiesByDomResourcePoolId.TryGetValue(id, out var poolCapabilities))
                {
                    result[id] = poolCapabilities;
                }
                else
                {
                    idsToRetrieve.Add(id);
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                foreach (var domResourcePool in GetResourcePoolsByIds(idsToRetrieve).Values)
                {
                    var poolCapabilities = domResourcePool.ResourcePoolCapabilities.OfType<IConfiguredCapability>().ToList();
                    result[domResourcePool.ID.Id] = poolCapabilities;
                    capabilitiesByDomResourcePoolId[domResourcePool.ID.Id] = poolCapabilities;
                }
            }

            return result;
        }

        private IDictionary<Guid, DomResourcePool> GetResourcePoolsByIds(IEnumerable<Guid> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException(nameof(poolIds));
            }

            var result = new Dictionary<Guid, DomResourcePool>();
            var idsToRetrieve = new List<Guid>();

            foreach (var id in poolIds.Where(x => x != Guid.Empty).Distinct())
            {
                if (domResourcePoolsById.TryGetValue(id, out var domResourcePool))
                {
                    result[id] = domResourcePool;
                }
                else
                {
                    idsToRetrieve.Add(id);
                }
            }

            if (idsToRetrieve.Count > 0)
            {
                foreach (var domResourcePool in planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(idsToRetrieve))
                {
                    result[domResourcePool.ID.Id] = domResourcePool;
                    domResourcePoolsById[domResourcePool.ID.Id] = domResourcePool;
                }
            }

            return result;
        }
        #endregion
    }
}