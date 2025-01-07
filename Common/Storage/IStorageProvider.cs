namespace Skyline.DataMiner.MediaOps.API.Common.Storage
{
    using System.Collections.Generic;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal interface IStorageProvider<T>
    {
        T Create(T oToCreate);

        IEnumerable<T> Read(FilterElement<T> filter);

        T Update(T oToUpdate);

        T Delete(T oToDelete);
    }
}
