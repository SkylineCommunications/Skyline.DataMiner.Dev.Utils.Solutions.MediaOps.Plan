namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public interface IDeleterRepository<T> where T : IApiObject
    {
        void Delete(params T[] objectApis);

        void Delete(params Guid[] objectIds);
    }
}
