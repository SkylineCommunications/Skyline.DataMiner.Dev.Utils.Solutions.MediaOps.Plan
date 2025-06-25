namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    public interface ICrudRepository<T> : ICreatorRepository<T>, IReaderRepository<T>, IUpdaterRepository<T>, IDeleterRepository<T> where T : IApiObject
    {
        IEnumerable<Guid> CreateOrUpdate(IEnumerable<T> apiObjects);
    }
}
