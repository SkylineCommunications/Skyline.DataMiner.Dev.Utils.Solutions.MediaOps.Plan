namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    public interface ICreatorRepository<T> where T: ApiObject
    {
        Guid Create(T apiObject);

        IEnumerable<Guid> Create(IEnumerable<T> apiObjects);
    }
}
