namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Implements <see cref="IPartialReadableRepository{T}"/> and <see cref="IPartialPageableRepository{T}"/> for API
	/// objects whose storage can't read a subset of fields (e.g. profile parameters). The objects are read in full and
	/// the selected exposers are evaluated on them, so callers can use the same API for every repository.
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

		public IEnumerable<Skyline.DataMiner.SDM.PartialObject<T>> ReadPartial(IPartialFilterElement<T> partialFilter)
		{
			var query = ToQuery(partialFilter);
			var fields = ValidateFields(partialFilter.Fields, nameof(partialFilter));

			return read(query).Select(x => Project(x, fields));
		}

		public IEnumerable<Skyline.DataMiner.SDM.PartialObject<T>> ReadPartial(IPartialQuery<T> partialQuery)
		{
			if (partialQuery == null)
			{
				throw new ArgumentNullException(nameof(partialQuery));
			}

			var fields = ValidateFields(partialQuery.Fields, nameof(partialQuery));

			return read(partialQuery).Select(x => Project(x, fields));
		}

		public IEnumerable<IPagedResult<Skyline.DataMiner.SDM.PartialObject<T>>> ReadPartialPaged(IPartialFilterElement<T> partialFilter, int pageSize)
		{
			var query = ToQuery(partialFilter);
			var fields = ValidateFields(partialFilter.Fields, nameof(partialFilter));

			return ProjectPages(readPaged(query, pageSize), fields, pageSize);
		}

		public IEnumerable<IPagedResult<Skyline.DataMiner.SDM.PartialObject<T>>> ReadPartialPaged(IPartialQuery<T> partialQuery, int pageSize)
		{
			if (partialQuery == null)
			{
				throw new ArgumentNullException(nameof(partialQuery));
			}

			var fields = ValidateFields(partialQuery.Fields, nameof(partialQuery));

			return ProjectPages(readPaged(partialQuery, pageSize), fields, pageSize);
		}

		/// <summary>
		/// Creates a partial object with the values the selected exposers return for the object. List exposers return all
		/// their values, other exposers return a single value.
		/// </summary>
		internal static Skyline.DataMiner.SDM.PartialObject<T> Project(T obj, IReadOnlyCollection<FieldExposer> fields)
		{
			if (obj == null)
			{
				throw new ArgumentNullException(nameof(obj));
			}

			return new Skyline.DataMiner.SDM.PartialObject<T>(
				obj.Id,
				fields.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x, GetValues(x, obj))));
		}

		private static IEnumerable<object> GetValues(FieldExposer exposer, T obj)
		{
			var value = exposer.execute(obj);

			if (IsListExposer(exposer))
			{
				return value is IEnumerable values ? values.Cast<object>().ToList() : new List<object>();
			}

			return value == null ? new List<object>() : new List<object> { value };
		}

		private static bool IsListExposer(FieldExposer exposer)
		{
			var type = exposer.GetType();
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DynamicListExposer<,>);
		}

		private static IQuery<T> ToQuery(IPartialFilterElement<T> partialFilter)
		{
			if (partialFilter == null)
			{
				throw new ArgumentNullException(nameof(partialFilter));
			}

			if (partialFilter.Filter == null)
			{
				throw new ArgumentException("The filter can't be null.", nameof(partialFilter));
			}

			return partialFilter.Filter.ToQuery();
		}

		private static IReadOnlyCollection<FieldExposer> ValidateFields(IReadOnlyCollection<FieldExposer> fields, string paramName)
		{
			if (fields == null || fields.Count == 0)
			{
				throw new ArgumentException("At least one field must be selected.", paramName);
			}

			var distinct = new List<FieldExposer>();
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var field in fields)
			{
				if (field == null || field.FilterType == null || !field.FilterType.IsAssignableFrom(typeof(T)))
				{
					throw new NotSupportedException($"Field '{field?.fieldName}' can't be read partially on {typeof(T).Name}.");
				}

				if (seen.Add(field))
				{
					distinct.Add(field);
				}
			}

			return distinct;
		}

		private static IEnumerable<IPagedResult<Skyline.DataMiner.SDM.PartialObject<T>>> ProjectPages(IEnumerable<IPagedResult<T>> pages, IReadOnlyCollection<FieldExposer> fields, int pageSize)
		{
			foreach (var page in pages)
			{
				yield return new PagedResult<Skyline.DataMiner.SDM.PartialObject<T>>(page.Select(x => Project(x, fields)).ToList(), page.PageNumber, pageSize, page.HasNextPage);
			}
		}
	}
}
