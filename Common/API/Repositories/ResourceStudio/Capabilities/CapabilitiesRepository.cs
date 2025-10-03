namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;

    internal class CapabilitiesRepository : Repository<Capability>, ICapabilitiesRepository
    {
        public CapabilitiesRepository(MediaOpsPlanApi api) : base(api)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountCapabilities();
        }

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

                if (!CoreCapabilitiesHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = result.SuccessfulIds.First();
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

                if (!CoreCapabilitiesHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = result.SuccessfulIds;
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
                if (!CoreCapabilitiesHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Created or Updated Capabilities Count", capabilityIds.Count);

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
                if (!CoreCapabilitiesHandler.TryDelete(PlanApi, capabilitiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = result.SuccessfulIds;
                act?.AddTag("Removed Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Removed Capabilities Count", capabilityIds.Count);
            });
        }

        public IQueryable<Capability> Query()
        {
            return new ApiRepositoryQuery<Capability>(QueryProvider);
        }

        public IQueryable<IEnumerable<Capability>> QueryPaged()
        {
            throw new NotImplementedException();
        }

        public Capability Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Capability with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapabilityId", id);
                var coreCapability = PlanApi.CoreHelpers.ProfileProvider.GetCapabilityById(id);

                if (coreCapability == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return new Capability(coreCapability);
            });
        }

        public IDictionary<Guid, Capability> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapabilityIds", String.Join(", ", ids));
                act?.AddTag("CapabilityIds Count", ids.Count());

                var capabilities = PlanApi.CoreHelpers.ProfileProvider.GetCapabilitiesById(ids);
                return capabilities.Select(x => new Capability(x)).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Capability> ReadAll()
        {
            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(ReadAll), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilities().Select(x => new Capability(x));
            });
        }

        public IEnumerable<IEnumerable<Capability>> ReadAllPaged()
        {
            throw new NotImplementedException();
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

                if (!CoreCapabilitiesHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = result.SuccessfulIds.First();
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
                    throw new InvalidOperationException("Not possible to use method Update for new resource properties. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapabilitiesHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = result.SuccessfulIds;
                act?.AddTag("CapabilityIds", String.Join(", ", capabilityIds));
            });
        }

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Capability> Read(IQuery<DomInstance> query)
        {
            throw new NotImplementedException();
        }
    }
}
