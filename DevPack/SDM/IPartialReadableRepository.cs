namespace Skyline.DataMiner.SDM
{
	using System.Collections.Generic;

	/// <summary>
	/// Defines a repository that supports reading a selected subset of fields of entities of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the entities.</typeparam>
	/// <remarks>
	/// Temporary definition that mirrors the interface announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// </remarks>
	public interface IPartialReadableRepository<T> : IRepositoryMarker<T>
		where T : class
	{
		/// <summary>
		/// Reads the selected fields of the entities that match the filter.
		/// </summary>
		/// <param name="partialFilter">The filter and the fields to return.</param>
		/// <returns>One partial object per matching entity.</returns>
		IEnumerable<PartialObject<T>> ReadPartial(IPartialFilterElement<T> partialFilter);

		/// <summary>
		/// Reads the selected fields of the entities that match the query.
		/// </summary>
		/// <param name="partialQuery">The query and the fields to return.</param>
		/// <returns>One partial object per matching entity.</returns>
		IEnumerable<PartialObject<T>> ReadPartial(IPartialQuery<T> partialQuery);
	}
}
