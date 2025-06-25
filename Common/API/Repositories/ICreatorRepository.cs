namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public interface ICreatorRepository<T> where T: IApiObject
    {
        Guid Create(T apiObject);
    }
}
