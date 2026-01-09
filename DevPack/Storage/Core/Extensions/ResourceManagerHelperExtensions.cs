namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.ResponseErrorData;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

    internal static class ResourceManagerHelperExtensions
    {
        public static IReadOnlyDictionary<Guid, ResourcePool> GetResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<Guid> resourcePoolIds)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePoolIds == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolIds));
            }

            if (!resourcePoolIds.Any())
            {
                return new Dictionary<Guid, ResourcePool>();
            }

            var coreResourcePools = new List<ResourcePool>();
            foreach (var batch in resourcePoolIds.Where(x => x != Guid.Empty).Distinct().Batch(500))
            {
                coreResourcePools.AddRange(helper.GetResourcePools(batch.Select(x => new ResourcePool(x)).ToArray()));
            }

            return coreResourcePools.ToDictionary(x => x.ID);
        }

        public static IReadOnlyDictionary<string, IReadOnlyCollection<ResourcePool>> GetResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<string> resourcePoolNames)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePoolNames == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolNames));
            }

            if (!resourcePoolNames.Any())
            {
                return new Dictionary<string, IReadOnlyCollection<ResourcePool>>();
            }

            var coreResourcePools = new List<ResourcePool>();
            foreach (var batch in resourcePoolNames.Where(x => !string.IsNullOrEmpty(x)).Distinct().Batch(500))
            {
                coreResourcePools.AddRange(helper.GetResourcePools(batch.Select(x => new ResourcePool { Name = x }).ToArray()));
            }

            return coreResourcePools
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<ResourcePool>)x.ToList());
        }

        public static BulkOperationResult<Guid> CreateOrUpdateResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var result = InnerCreateOrUpdateResourcePoolsInBatches(helper, resourcePools);
            result.ThrowBulkException();

            return result;
        }

        public static bool TryCreateOrUpdateResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools, out BulkOperationResult<Guid> result)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            result = InnerCreateOrUpdateResourcePoolsInBatches(helper, resourcePools);

            return !result.HasFailures;
        }

        public static Exceptions.BulkOperationResult<Guid> DeleteResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var result = InnerDeleteResourcePoolsInBatches(helper, resourcePools);
            result.ThrowBulkException();

            return result;
        }

        public static bool TryDeleteResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools, out Exceptions.BulkOperationResult<Guid> result)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            result = InnerDeleteResourcePoolsInBatches(helper, resourcePools);

            return !result.HasFailures;
        }

        public static IEnumerable<Resource> GetResources<T>(this ResourceManagerHelper helper, IEnumerable<T> values, Func<T, FilterElement<Resource>> filter)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return FilterQueryExecutor.RetrieveFilteredItems(
                values.Distinct(),
                x => filter(x),
                x => helper.GetResources(x));
        }

        public static BulkOperationResult<Guid> CreateOrUpdateResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            var result = InnerCreateOrUpdateResourcesInBatches(helper, resources, out _);
            result.ThrowBulkException();

            return result;
        }

        public static BulkOperationResult<Guid> CreateOrUpdateResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, out IEnumerable<Resource> createdOrUpdatedResources)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            var result = InnerCreateOrUpdateResourcesInBatches(helper, resources, out createdOrUpdatedResources);
            result.ThrowBulkException();

            return result;
        }

        public static bool TryCreateOrUpdateResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, out BulkOperationResult<Guid> result)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            result = InnerCreateOrUpdateResourcesInBatches(helper, resources, out _);

            return !result.HasFailures;
        }

        // Todo: Needs refactoring to avoid 2 out parameters???
        public static bool TryCreateOrUpdateResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, out BulkOperationResult<Guid> result, out IEnumerable<Resource> createdOrUpdatedResources)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            result = InnerCreateOrUpdateResourcesInBatches(helper, resources, out createdOrUpdatedResources);

            return !result.HasFailures;
        }

        public static Exceptions.BulkOperationResult<Guid> DeleteResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, ResourceDeleteOptions options)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var result = InnerDeleteResourcesInBatches(helper, resources, options);
            result.ThrowBulkException();

            return result;
        }

        public static bool TryDeleteResourcesInBatches(this ResourceManagerHelper helper, IEnumerable<Resource> resources, ResourceDeleteOptions options, out Exceptions.BulkOperationResult<Guid> result)
        {
            if (helper == null)
            {
                throw new ArgumentNullException(nameof(helper));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            result = InnerDeleteResourcesInBatches(helper, resources, options);

            return !result.HasFailures;
        }

        private static BulkOperationResult<Guid> InnerCreateOrUpdateResourcePoolsInBatches(ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
        {
            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            foreach (var batch in resourcePools.Batch(100))
            {
                var pools = helper.AddOrUpdateResourcePools(batch.ToArray());

                successfulIds.AddRange(pools.Select(x => x.ID));

                var traceData = helper.GetTraceDataLastCall();
                foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
                {
                    if (!error.SubjectId.HasValue)
                    {
                        continue;
                    }

                    if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
                    {
                        mediaOpsTraceData = new MediaOpsTraceData();
                        traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

                        unsuccessfulIds.Add(error.SubjectId.Value);
                    }

                    mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
                }
            }

            return new BulkOperationResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
        }

        private static Exceptions.BulkOperationResult<Guid> InnerDeleteResourcePoolsInBatches(ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
        {
            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerDeleteResourcePoolsInBatches), act =>
            {
                foreach (var batch in resourcePools.Batch(100))
                {
                    var res = helper.RemoveResourcePools(batch.ToArray());

                    successfulIds.AddRange(res.Select(x => x.ID));

                    var traceData = helper.GetTraceDataLastCall();
                    foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
                    {
                        if (!error.SubjectId.HasValue)
                        {
                            continue;
                        }

                        if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
                        {
                            mediaOpsTraceData = new MediaOpsTraceData();
                            traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

                            unsuccessfulIds.Add(error.SubjectId.Value);
                        }

                        mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
                    }
                }
            });

            return new Exceptions.BulkOperationResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
        }

        private static BulkOperationResult<Guid> InnerCreateOrUpdateResourcesInBatches(ResourceManagerHelper helper, IEnumerable<Resource> resources, out IEnumerable<Resource> createdOrUpdatedResources)
        {
            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            var createdOrUpdated = new List<Resource>();

            ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerCreateOrUpdateResourcesInBatches), act =>
            {
                foreach (var batch in resources.Batch(100))
                {
                    var res = helper.AddOrUpdateResources(batch.ToArray());

                    createdOrUpdated.AddRange(res);
                    successfulIds.AddRange(res.Select(x => x.ID));

                    var traceData = helper.GetTraceDataLastCall();
                    foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
                    {
                        if (!error.SubjectId.HasValue)
                        {
                            continue;
                        }

                        if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
                        {
                            mediaOpsTraceData = new MediaOpsTraceData();
                            traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

                            unsuccessfulIds.Add(error.SubjectId.Value);
                        }

                        mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
                    }
                }
            });

            createdOrUpdatedResources = createdOrUpdated;

            return new BulkOperationResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
        }

        private static Exceptions.BulkOperationResult<Guid> InnerDeleteResourcesInBatches(ResourceManagerHelper helper, IEnumerable<Resource> resources, ResourceDeleteOptions options)
        {
            var successfulIds = new List<Guid>();
            var unsuccessfulIds = new List<Guid>();
            var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

            ActivityHelper.Track(nameof(ResourceManagerHelperExtensions), nameof(InnerDeleteResourcesInBatches), act =>
            {
                foreach (var batch in resources.Batch(100))
                {
                    var res = helper.RemoveResources(batch.ToArray(), options);

                    successfulIds.AddRange(res.Select(x => x.ID));

                    var traceData = helper.GetTraceDataLastCall();
                    foreach (var error in traceData.ErrorData.OfType<ResourceManagerErrorData>())
                    {
                        if (!error.SubjectId.HasValue)
                        {
                            continue;
                        }

                        if (!traceDataPerItem.TryGetValue(error.SubjectId.Value, out var mediaOpsTraceData))
                        {
                            mediaOpsTraceData = new MediaOpsTraceData();
                            traceDataPerItem.Add(error.SubjectId.Value, mediaOpsTraceData);

                            unsuccessfulIds.Add(error.SubjectId.Value);
                        }

                        mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = error.ToString() });
                    }
                }
            });

            return new Exceptions.BulkOperationResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
        }
    }
}
