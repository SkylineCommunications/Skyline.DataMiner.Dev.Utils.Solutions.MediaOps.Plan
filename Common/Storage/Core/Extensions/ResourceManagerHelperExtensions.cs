namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.ResponseErrorData;

    internal static class ResourceManagerHelperExtensions
    {
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
