namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using SLDataGateway.API.Types.Querying;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class CapabilitiesRepository : ProfileParameterRepository<Capability>, ICapabilitiesRepository
    {
        public CapabilitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long Count()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapabilities();
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

        public IEnumerable<Capability> Read()
        {
            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Read), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilities().Select(x => new Capability(x));
            });
        }

        public IEnumerable<IPagedResult<Capability>> ReadPaged()
        {
            int pageSize = 500;
            var pageNumber = 0;
            var items = PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilitiesPaged(pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Capability>(page.Select(x => new Capability(x)), pageNumber++, pageSize, hasNext);
            }
        }

        public IEnumerable<IPagedResult<Capability>> ReadPaged(FilterElement<Capability> filter)
        {
            int pageSize = 500;
            var pageNumber = 0;
            var items = PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilitiesPaged(filter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Capability>(page.Select(x => new Capability(x)), pageNumber++, pageSize, hasNext);
            }
        }

        public IEnumerable<IPagedResult<Capability>> ReadPaged(IQuery<Capability> query)
        {
            int pageSize = 500;
            var pageNumber = 0;
            var items = PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilitiesPaged(query, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Capability>(page.Select(x => new Capability(x)), pageNumber++, pageSize, hasNext);
            }
        }

        public IEnumerable<IPagedResult<Capability>> ReadPaged(FilterElement<Capability> filter, int pageSize)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPagedResult<Capability>> ReadPaged(IQuery<Capability> query, int pageSize)
        {
            throw new NotImplementedException();
        }

        internal override long Count(FilterElement<Net.Profiles.Parameter> filter)
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountCapabilities(filter);
        }

        internal override IEnumerable<Capability> Read(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return PlanApi.CoreHelpers.ProfileProvider.GetCapabilities(query).Select(x => new Capability(x));
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

        public IEnumerable<Capability> Read(FilterElement<Capability> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Capability> Read(IQuery<Capability> query)
        {
            throw new NotImplementedException();
        }

        public long Count(FilterElement<Capability> filter)
        {
            throw new NotImplementedException();
        }

        public long Count(IQuery<Capability> query)
        {
            throw new NotImplementedException();
        }
    }
}
