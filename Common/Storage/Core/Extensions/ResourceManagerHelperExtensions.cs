namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.ResponseErrorData;

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

        public static BulkCreateOrUpdateResult<Guid> CreateOrUpdateResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
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
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryCreateOrUpdateResourcePoolsInBatches(this ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools, out BulkCreateOrUpdateResult<Guid> result)
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

            return !result.HasFailures();
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

        private static BulkCreateOrUpdateResult<Guid> InnerCreateOrUpdateResourcePoolsInBatches(ResourceManagerHelper helper, IEnumerable<ResourcePool> resourcePools)
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

            return new BulkCreateOrUpdateResult<Guid>(successfulIds, unsuccessfulIds, traceDataPerItem);
        }
    }
}
