namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.SDM;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Implements <see cref="IPartialReadableRepository{T}"/> and <see cref="IPartialPageableRepository{T}"/> for API
	/// objects whose storage can't read a subset of fields (e.g. profile parameters). The objects are read in full and
	/// the selected fields are projected from them, so callers can use the same API for every repository.
	/// </summary>
	/// <typeparam name="T">The type of the API object.</typeparam>
	internal sealed class ProjectionPartialReader<T>
		where T : ApiObject
	{
		private readonly Func<IQuery<T>, IEnumerable<T>> read;
		private readonly Func<IQuery<T>, int, IEnumerable<IPagedResult<T>>> readPaged;

		public ProjectionPartialReader(Func<IQuery<T>, IEnumerable<T>> read, Func<IQuery<T>, int, IEnumerable<IPagedResult<T>>> readPaged)
		{
			this.read = read ?? throw new ArgumentNullException(nameof(read));
			this.readPaged = readPaged ?? throw new ArgumentNullException(nameof(readPaged));
		}

		public IEnumerable<PartialObject<T>> ReadPartial(IPartialQuery<T> partialQuery)
		{
			var (query, fields) = Deconstruct(partialQuery);

			return read(query).Select(x => Project(x, fields));
		}

		public IEnumerable<IPagedResult<PartialObject<T>>> ReadPartialPaged(IPartialQuery<T> partialQuery, int pageSize)
		{
			var (query, fields) = Deconstruct(partialQuery);

			return ProjectPages(readPaged(query, pageSize), fields, pageSize);
		}

		/// <summary>
		/// Creates a partial object with the values the selected fields have on the object.
		/// </summary>
		internal static PartialObject<T> Project(T obj, IReadOnlyCollection<IFieldProjection<T>> fields)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			var builder = PartialObject<T>.CreateBuilder();

			foreach (var field in fields)
			{
				field.Populate(builder, obj);
			}

			return builder.Build();
		}

		private static (IQuery<T> Query, IReadOnlyCollection<IFieldProjection<T>> Fields) Deconstruct(IPartialQuery<T> partialQuery)
		{
			if (partialQuery == null)
			{
				throw new ArgumentNullException(nameof(partialQuery));
			}

			if (partialQuery.Query?.Filter == null)
			{
				throw new ArgumentException("The query and its filter can't be null.", nameof(partialQuery));
			}

			var fields = partialQuery.Fields?.Where(x => x != null).ToList();
			if (fields == null || fields.Count == 0)
			{
				throw new ArgumentException("At least one field must be selected.", nameof(partialQuery));
			}

			return (partialQuery.Query, fields);
		}

		private static IEnumerable<IPagedResult<PartialObject<T>>> ProjectPages(IEnumerable<IPagedResult<T>> pages, IReadOnlyCollection<IFieldProjection<T>> fields, int pageSize)
		{
			foreach (var page in pages)
			{
				yield return new PagedResult<PartialObject<T>>(page.Select(x => Project(x, fields)).ToList(), page.PageNumber, pageSize, page.HasNextPage);
			}
		}
	}
}
