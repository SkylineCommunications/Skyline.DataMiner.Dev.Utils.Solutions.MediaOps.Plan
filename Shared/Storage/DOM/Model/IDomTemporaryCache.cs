namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System.Collections.Generic;

    internal interface IDomTemporaryCache
    {
        void SetCache<T>(IEnumerable<T> instances) where T : DomInstanceBase;

        void AddToCache<T>(IEnumerable<T> instances) where T : DomInstanceBase;

        IEnumerable<T> GetFromCache<T>() where T : DomInstanceBase;
    }
}
