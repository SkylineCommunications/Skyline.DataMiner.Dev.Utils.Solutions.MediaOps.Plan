namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal partial class CapacitiesRepository : ProfileParameterRepository<Capacity>, ICapacitiesRepository
    {
        public CapacitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapacities();
        }

        public IQueryable<Capacity> Query()
        {
            return new ApiRepositoryQuery<Capacity, Net.Profiles.Parameter>(QueryProvider);
        }

        public Capacity Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Capacity with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityId", id);
                var coreCapacity = PlanApi.CoreHelpers.ProfileProvider.GetCapacityById(id);

                if (coreCapacity == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return new Capacity(coreCapacity);
            });
        }

        public IDictionary<Guid, Capacity> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityIds", String.Join(", ", ids));
                act?.AddTag("CapacityIds Count", ids.Count());

                var capacities = PlanApi.CoreHelpers.ProfileProvider.GetCapacitiesById(ids);
                return capacities.Select(x => new Capacity(x)).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Capacity> ReadAll()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(ReadAll), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapacities().Select(x => new Capacity(x));
            });
        }

        public IEnumerable<IEnumerable<Capacity>> ReadAllPaged()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(ReadAllPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapacitiesPaged().Select(page => page.Select(x => new Capacity(x)));
            });
        }

        internal override long Count(FilterElement<Net.Profiles.Parameter> filter)
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountCapacities(filter);
        }

        internal override IEnumerable<Capacity> Read(IQuery<Net.Profiles.Parameter> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return PlanApi.CoreHelpers.ProfileProvider.GetCapacities(query).Select(x => new Capacity(x));
        }

        protected internal override FilterElement<Net.Profiles.Parameter> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(Capacity.Units):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Units, comparer, value);
                case nameof(Capacity.RangeMin):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.RangeMin, comparer, Convert.ToDouble(value));
                case nameof(Capacity.RangeMax):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.RangeMax, comparer, Convert.ToDouble(value));
                case nameof(Capacity.StepSize):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Stepsize, comparer, Convert.ToDouble(value));
                case nameof(Capacity.Decimals):
                    return FilterElementFactory<Net.Profiles.Parameter>.Create(Net.Profiles.ParameterExposers.Decimals, comparer, value);
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(Capacity.Units):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Units, sortOrder, naturalSort);
                case nameof(Capacity.RangeMin):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.RangeMin, sortOrder, naturalSort);
                case nameof(Capacity.RangeMax):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.RangeMax, sortOrder, naturalSort);
                case nameof(Capacity.StepSize):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Stepsize, sortOrder, naturalSort);
                case nameof(Capacity.Decimals):
                    return OrderByElementFactory.Create(Net.Profiles.ParameterExposers.Decimals, sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }
    }
}
