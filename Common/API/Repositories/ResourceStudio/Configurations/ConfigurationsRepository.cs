namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    internal class ConfigurationsRepository : ProfileParameterRepository<Configuration>, IConfigurationsRepository
    {
        public ConfigurationsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            throw new NotImplementedException();
        }

        public Guid Create(Configuration apiObject)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> Create(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Configuration[] apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException();
        }

        public IQueryable<Configuration> Query()
        {
            throw new NotImplementedException();
        }

        public Configuration Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, Configuration> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Configuration> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<Configuration>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public void Update(Configuration apiObject)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<Configuration> apiObjects)
        {
            throw new NotImplementedException();
        }

        internal override long Count(FilterElement<Net.Profiles.Parameter> domFilter)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Configuration> Read(IQuery<Net.Profiles.Parameter> query)
        {
            throw new NotImplementedException();
        }
    }
}
