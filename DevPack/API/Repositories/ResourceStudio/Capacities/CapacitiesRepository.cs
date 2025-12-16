namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using SLDataGateway.API.Types.Querying;

    internal class CapacitiesRepository : ProfileParameterRepository<Capacity>, ICapacitiesRepository
    {
        public CapacitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long Count()
        {
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapacities();
        }

        public long Count(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        public long Count(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
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
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Create), act =>
            {
                if (apiObjects.Any(x => !x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing capacities. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", string.Join(", ", capacityIds));

                return capacityIds;
            });
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Created or Updated Capacities Count", capacityIds.Count());

                return capacityIds;
            });
        }

        public void Delete(params Capacity[] apiObjects)
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

            var capacitiesToDelete = Read(apiObjectIds).Values;

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapacityHandler.TryDelete(PlanApi, capacitiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjectIds;
                act?.AddTag("Removed Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Removed Capacities Count", capacityIds.Count());
            });
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
                return Capacity.InstantiateCapacities([coreCapacity]).FirstOrDefault();
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
                return Capacity.InstantiateCapacities(capacities).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Capacity> Read()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                return Capacity.InstantiateCapacities(PlanApi.CoreHelpers.ProfileProvider.GetAllCapacities());
            });
        }

        public IEnumerable<Capacity> Read(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Capacity> Read(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<Capacity>> ReadPaged()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(ReadPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapacitiesPaged().Select(page => Capacity.InstantiateCapacities(page));
            });
        }
        IEnumerable<IPagedResult<Capacity>> IPageableRepository<Capacity>.ReadPaged()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter, int pageSize)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query, int pageSize)
        {
            throw new NotImplementedException();
        }

        public void Update(Capacity apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing capacity {apiObject.Name}...");

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capacity. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capacityId = apiObject.Id;
                act?.AddTag("CapacityId", capacityId);
            });
        }

        public void Update(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Update), act =>
            {
                if (apiObjects.Any(x => x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capacities. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", String.Join(", ", capacityIds));
            });
        }
    }
}
