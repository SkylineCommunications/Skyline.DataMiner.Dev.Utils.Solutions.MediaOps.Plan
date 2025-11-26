namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    internal partial class ConfigurationsRepository : ProfileParameterRepository<Configuration>, IConfigurationsRepository
    {
        public Guid Create(Configuration apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> Create(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Configuration[] apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(Configuration apiObject)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }

        public void Update(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException("Requires InterApp implementation");
        }
    }
}
