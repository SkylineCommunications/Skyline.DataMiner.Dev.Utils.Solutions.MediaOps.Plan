namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System.Collections.Generic;

	/// <summary>
	/// Defines methods to update API objects in a repository.
	/// </summary>
	/// <typeparam name="T">The type of API object to update. Must inherit from <see cref="ApiObject"/>.</typeparam>
	public interface IUpdaterRepository<in T> where T : ApiObject
	{
		/// <summary>
		/// Updates the specified API object in the repository.
		/// </summary>
		/// <param name="apiObject">The API object to update.</param>
		void Update(T apiObject);

		/// <summary>
		/// Updates the specified collection of API objects in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of API objects to update.</param>
		void Update(IEnumerable<T> apiObjects);
	}
}
