namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    public interface IReaderRepository<T> where T : ApiObject
    {
        IEnumerable<T> ReadAll();

        IEnumerable<IEnumerable<T>> ReadAllPage();

        T Read(Guid id);

        IDictionary<Guid, T> Read(IEnumerable<Guid> ids);

        IEnumerable<T> Read(FilterElement<T> filter);
    }
}
