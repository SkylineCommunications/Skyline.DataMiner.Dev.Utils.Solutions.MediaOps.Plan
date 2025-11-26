namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal partial class CapabilitiesRepository : ProfileParameterRepository<Capability>, ICapabilitiesRepository
    {
        public CapabilitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapabilities();
        }

        public IQueryable<Capability> Query()
        {
            return new ApiRepositoryQuery<Capability, Net.Profiles.Parameter>(QueryProvider);
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
