namespace Skyline.DataMiner.SDM
{
	using System.Collections.Generic;

	/// <summary>
	/// Defines a repository that supports reading a selected subset of fields of entities of type <typeparamref name="T"/> page by page.
	/// </summary>
	/// <typeparam name="T">The type of the entities.</typeparam>
	/// <remarks>
	/// Temporary definition that mirrors the interface announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// </remarks>
	public interface IPartialPageableRepository<T> : IPartialReadableRepository<T>
		where T : class
	{
		/// <summary>
		/// Reads the selected fields of the entities that match the filter, page by page.
		/// </summary>
		/// <param name="partialFilter">The filter and the fields to return.</param>
		/// <returns>The pages of partial objects.</returns>
		IEnumerable<IPagedResult<PartialObject<T>>> ReadPartialPaged(IPartialFilterElement<T> partialFilter);

		/// <summary>
		/// Reads the selected fields of the entities that match the query, page by page.
		/// </summary>
		/// <param name="partialQuery">The query and the fields to return.</param>
		/// <returns>The pages of partial objects.</returns>
		IEnumerable<IPagedResult<PartialObject<T>>> ReadPartialPaged(IPartialQuery<T> partialQuery);
	}
}
