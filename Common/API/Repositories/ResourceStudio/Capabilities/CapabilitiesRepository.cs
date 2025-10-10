namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal class CapabilitiesRepository : ProfileParameterRepository<Capability>, ICapabilitiesRepository
    {
        public CapabilitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapabilities();
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

        public IQueryable<Capability> Query()
        {
            return new ApiRepositoryQuery<Capability, Net.Profiles.Parameter>(QueryProvider);
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
            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(ReadAllPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilitiesPaged().Select(page => page.Select(x => new Capability(x)));
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

        protected internal override FilterElement<Net.Profiles.Parameter> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(Capability.IsMandatory):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.IsOptional, comparer, !Convert.ToBoolean(value));
                case nameof(Capability.IsTimeDependent):
                    return IsTimeDependantFilter(comparer, Convert.ToBoolean(value));
                case nameof(Capability.Discretes):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Discretes, comparer, value);
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(Capability.IsMandatory):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.IsOptional, sortOrder, naturalSort);
                case nameof(Capability.IsTimeDependent):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Remarks, sortOrder, naturalSort);
                case nameof(Capability.Discretes):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Discretes, sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        private FilterElement<Net.Profiles.Parameter> IsTimeDependantFilter(Comparer comparer, bool value)
        {
            bool isTimeDependentCheck;
            switch (comparer)
            {
                case Comparer.Equals:
                    isTimeDependentCheck = value;
                    break;
                case Comparer.NotEquals:
                    isTimeDependentCheck = !value;
                    break;
                default:
                    throw new NotSupportedException($"Comparer {comparer} is not supported for boolean TimeDependency checks");
            }

            if (isTimeDependentCheck)
            {
                return Net.Profiles.ParameterExposers.Remarks.Contains("\"isTimeDependent\":true");
            }
            else
            {
                return Net.Profiles.ParameterExposers.Remarks.NotContains("\"isTimeDependent\":true");
            }
        }
    }
}
