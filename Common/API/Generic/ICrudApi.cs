namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines that the implementing class can create new objects.
    /// </summary>
    /// <typeparam name="TObject">The type to request a create.</typeparam>
    public interface ICrudApi<TObject>
        where TObject : IApiObject
    {
        /// <summary>
        /// Request to add new instances. The method will be blocking until all requests are handled.
        /// </summary>
        /// <param name="objectToCreate">The create requests.</param>
        Guid Create(TObject objectToCreate);

        void Update(TObject objectToUpdate);

        IEnumerable<Guid> CreateOrUpdate(IEnumerable<TObject> objectsToCreate);

        /// <summary>
        /// Request to delete instances. The method will be blocking until all requests are handled.
        /// </summary>
        /// <param name="objectsToDelete">The objects to delete.</param>
        void Delete(params TObject[] objectsToDelete);

        void Delete(params Guid[] objectIdsToDelete);
    }
}