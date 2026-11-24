namespace Skyline.DataMiner.SDM
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Provides methods to select the fields that a partial read should return.
	/// </summary>
	/// <remarks>
	/// Temporary definition that mirrors the API announced by the SDM team. Remove once it ships in Skyline.DataMiner.Dev.Utils.SDM.Abstractions.
	/// </remarks>
	public static class PartialReadExtensions
	{
		/// <summary>
		/// Selects the fields to return for the objects that match the filter.
		/// </summary>
		/// <typeparam name="T">The type of the objects.</typeparam>
		/// <param name="filter">The filter that selects the objects.</param>
		/// <param name="fields">The fields to return. Duplicates are ignored.</param>
		/// <returns>The filter combined with the selected fields.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="filter"/>, <paramref name="fields"/> or one of the fields is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when no field is selected or a field doesn't belong to <typeparamref name="T"/>.</exception>
		public static IPartialFilterElement<T> WithFields<T>(this FilterElement<T> filter, params FieldExposer[] fields)
		{
			return WithFields(filter, (IEnumerable<FieldExposer>)fields);
		}

		/// <inheritdoc cref="WithFields{T}(FilterElement{T}, FieldExposer[])"/>
		public static IPartialFilterElement<T> WithFields<T>(this FilterElement<T> filter, IEnumerable<FieldExposer> fields)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return new PartialFilterElement<T>(filter, FieldSelection.Create<T>(fields, nameof(fields)));
		}

		/// <summary>
		/// Selects the fields to return for the objects that match the query.
		/// When the query already selects fields, the specified fields are added to them.
		/// </summary>
		/// <typeparam name="T">The type of the objects.</typeparam>
		/// <param name="query">The query that selects, orders and limits the objects.</param>
		/// <param name="fields">The fields to return. Duplicates are ignored.</param>
		/// <returns>The query combined with the selected fields.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="query"/>, <paramref name="fields"/> or one of the fields is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when no field is selected or a field doesn't belong to <typeparamref name="T"/>.</exception>
		public static IPartialQuery<T> WithFields<T>(this IQuery<T> query, params FieldExposer[] fields)
		{
			return WithFields(query, (IEnumerable<FieldExposer>)fields);
		}

		/// <inheritdoc cref="WithFields{T}(IQuery{T}, FieldExposer[])"/>
		public static IPartialQuery<T> WithFields<T>(this IQuery<T> query, IEnumerable<FieldExposer> fields)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query is IPartialQuery<T> partialQuery)
			{
				return new PartialQuery<T>(partialQuery, FieldSelection.Merge<T>(partialQuery.Fields, fields, nameof(fields)));
			}

			return new PartialQuery<T>(query, FieldSelection.Create<T>(fields, nameof(fields)));
		}
	}
}
