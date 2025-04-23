namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System.Linq;

    public interface IApiQueryable<T>
        where T : IApiObject
    {
        IOrderedQueryable<T> Query();
    }
}
