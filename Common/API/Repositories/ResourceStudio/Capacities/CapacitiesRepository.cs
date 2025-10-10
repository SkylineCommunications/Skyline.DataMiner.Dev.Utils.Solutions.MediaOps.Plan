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

    internal class CapacitiesRepository : ProfileParameterRepository<Capacity>, ICapacitiesRepository
    {
        public CapacitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapacities();
        }

        public Guid Create(Capacity apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Capacity...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for an existing capacity. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capacityId = apiObject.Id;
                act?.AddTag("CapacityId", capacityId);

                return capacityId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Capacity[] apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException();
        }

        public IQueryable<Capacity> Query()
        {
            throw new NotImplementedException();
        }

        public IQueryable<IEnumerable<Capacity>> QueryPaged()
        {
            throw new NotImplementedException();
        }

        public Capacity Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, Capacity> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Capacity> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<Capacity>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public void Update(Capacity apiObject)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException();
        }

        internal override long Count(FilterElement<Net.Profiles.Parameter> domFilter)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Capacity> Read(IQuery<Net.Profiles.Parameter> query)
        {
            throw new NotImplementedException();
        }
    }
}
