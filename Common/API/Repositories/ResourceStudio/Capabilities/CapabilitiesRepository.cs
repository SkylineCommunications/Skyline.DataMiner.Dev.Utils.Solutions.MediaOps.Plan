namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

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

        }

        public IEnumerable<Guid> Create(IEnumerable<Capability> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Capability> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Capability[] apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException();
        }

        public IQueryable<Capability> Query()
        {
            throw new NotImplementedException();
        }

        public IQueryable<IEnumerable<Capability>> QueryPaged()
        {
            throw new NotImplementedException();
        }

        public Capability Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, Capability> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Capability> ReadAll()
        {
            return PlanApi.CoreHelpers.ProfileProvider.GetAllCapabilities().Select(x => new Capability(x));
        }

        public IEnumerable<IEnumerable<Capability>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public void Update(Capability apiObject)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<Capability> apiObjects)
        {
            throw new NotImplementedException();
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
