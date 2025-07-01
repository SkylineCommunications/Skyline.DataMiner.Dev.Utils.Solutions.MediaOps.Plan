namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

	/// <summary>
	/// Defines methods to delete API objects or their identifiers from a repository.
	/// </summary>
	/// <typeparam name="T">The type of API object, derived from <see cref="ApiObject"/>.</typeparam>
	public interface IDeleterRepository<T> where T : ApiObject
	{
		/// <summary>
		/// Deletes the specified API objects from the repository.
		/// </summary>
		/// <param name="objectApis">The API objects to delete.</param>
		void Delete(params T[] objectApis);

		/// <summary>
		/// Deletes the API objects with the specified unique identifiers from the repository.
		/// </summary>
		/// <param name="objectIds">The unique identifiers of the API objects to delete.</param>
		void Delete(params Guid[] objectIds);
	}
}
