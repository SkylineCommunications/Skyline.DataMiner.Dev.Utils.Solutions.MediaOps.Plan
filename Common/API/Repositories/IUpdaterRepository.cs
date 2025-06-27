namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System.Collections.Generic;

    public interface IUpdaterRepository<T> where T : ApiObject
    {
        void Update(T apiObject);

        void Update(IEnumerable<T> apiObjects);
    }
}
