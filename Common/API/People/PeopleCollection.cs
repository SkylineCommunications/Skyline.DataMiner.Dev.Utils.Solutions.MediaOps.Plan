namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    internal class PeopleCollection : IPeopleCollection
    {
        internal MediaOpsHelper Helper { get; }

        internal DomDefinitionPeople DomDefinitionPeople { get; }

        public PeopleCollection(MediaOpsHelper helper)
        {
            Helper = helper;
            DomDefinitionPeople = new DomDefinitionPeople(Helper.Module_PnO);
        }

        public IOrderedQueryable<IPerson> Query()
        {
            return DomDefinitionPeople.Query();
        }

        public void CreateAsync(IEnumerable<CreatePersonRequest> requests)
        {
            var instances = requests.Select(x => x.ToInstance()).ToList();
            Helper.Module_PnO.DomInstances.CreateOrUpdate(instances);
        }

        public ResultMessage<CreatePersonRequest> Create(IEnumerable<CreatePersonRequest> requests)
        {
            var instances = requests.Select(x => new { request = x, instance = x.ToInstance() }).ToList();
            var result = Helper.Module_PnO.DomInstances.CreateOrUpdate(instances.Select(x => x.instance).ToList());
            if (result.UnsuccessfulIds.Any())
            {
                return new ResultMessage<CreatePersonRequest>
                {
                    Succeeded = false,
                    FailedRequests = instances.Where(x => result.UnsuccessfulIds.Contains(x.instance.ID)).Select(x => x.request).ToArray()
                };
            }
            else
            {
                return new ResultMessage<CreatePersonRequest>
                {
                    Succeeded = true,
                    FailedRequests = Array.Empty<CreatePersonRequest>()
                };
            }
        }
    }
}