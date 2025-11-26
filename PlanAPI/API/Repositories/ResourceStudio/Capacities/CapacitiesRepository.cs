namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    internal partial class CapacitiesRepository : ProfileParameterRepository<Capacity>, ICapacitiesRepository
    {
        public Guid Create(Capacity apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> Create(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Capacity[] apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(Capacity apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(IEnumerable<Capacity> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }
    }
}
