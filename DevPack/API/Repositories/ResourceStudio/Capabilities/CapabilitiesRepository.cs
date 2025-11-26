namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal partial class CapabilitiesRepository : ProfileParameterRepository<Capability>, ICapabilitiesRepository
    {
        public Guid Create(Capability apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Capability...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for an existing capability. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = apiObject.Id;
                act?.AddTag("CapabilityId", capabilityId);

                return capabilityId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Create), act =>
            {
                var existingCapabilities = apiObjects.Where(x => !x.IsNew);
                if (existingCapabilities.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing capabilities. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapabilityIds", string.Join(", ", capabilityIds));

                return capabilityIds;
            });
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Created or Updated Capabilities Count", capabilityIds.Count());

                return capabilityIds;
            });
        }

        public void Delete(params Capability[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var capabilitiesToDelete = Read(apiObjectIds).Values;

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapabilityHandler.TryDelete(PlanApi, capabilitiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjectIds;
                act?.AddTag("Removed Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Removed Capabilities Count", capabilityIds.Count());
            });
        }

        public void Update(Capability apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing capability {apiObject.Name}...");

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capability. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = apiObject.Id;
                act?.AddTag("CapabilityId", capabilityId);
            });
        }

        public void Update(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Update), act =>
            {
                var newCapabilities = apiObjects.Where(x => x.IsNew);
                if (newCapabilities.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capabilities. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapabilityIds", String.Join(", ", capabilityIds));
            });
        }
    }
}
