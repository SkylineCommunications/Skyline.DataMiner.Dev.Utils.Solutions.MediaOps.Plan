namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using SLDataGateway.API.Types.Querying;

    internal class CapacitiesRepository : Repository<Capacity>, ICapacitiesRepository
    {
        public CapacitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long CountAll()
        {
            throw new NotImplementedException();
        }

        public Guid Create(Capacity apiObject)
        {
            throw new NotImplementedException();
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

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Capacity> Read(IQuery<DomInstance> query)
        {
            throw new NotImplementedException();
        }
    }
}
