namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public interface IUpdaterRepository<T> where T : IApiObject
    {
        Guid Update(T apiObject);
    }
}
