namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Implements <see cref="IPartialReadableRepository{T}"/> and <see cref="IPartialPageableRepository{T}"/> for an API
	/// object that is stored in DOM, using DOM select reads so only the needed DOM fields are transferred.
	/// </summary>
	/// <typeparam name="T">The type of the API object.</typeparam>
	internal sealed class DomPartialReader<T>
		where T : ApiObject
	{
		/// <summary>
		/// The number of objects converted at once, so data stored outside the DOM instances is read once per batch instead of per object.
		/// </summary>
		internal const int BatchSize = 100;

		private readonly MediaOpsPlanApi planApi;
		private readonly Func<DomHelper> getDomHelper;
		private readonly FilterTranslator<T, DomInstance> filterTranslator;
		private readonly IDomPartialFieldMapper<T> fieldMapper;
		private readonly string activityName;

		public DomPartialReader(MediaOpsPlanApi planApi, Func<DomHelper> getDomHelper, FilterTranslator<T, DomInstance> filterTranslator, IDomPartialFieldMapper<T> fieldMapper, string activityName)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.getDomHelper = getDomHelper ?? throw new ArgumentNullException(nameof(getDomHelper));
			this.filterTranslator = filterTranslator ?? throw new ArgumentNullException(nameof(filterTranslator));
			this.fieldMapper = fieldMapper ?? throw new ArgumentNullException(nameof(fieldMapper));
			this.activityName = activityName ?? throw new ArgumentNullException(nameof(activityName));
		}

		public IEnumerable<PartialObject<T>> ReadPartial(IPartialQuery<T> partialQuery)
		{
			var (query, fields) = Deconstruct(partialQuery);

			return Read(query, fields);
		}

		public IEnumerable<IPagedResult<PartialObject<T>>> ReadPartialPaged(IPartialQuery<T> partialQuery, int pageSize)
		{
			var (query, fields) = Deconstruct(partialQuery);

			return ReadPaged(query, fields, pageSize);
		}

		internal static IEnumerable<List<TItem>> Batch<TItem>(IEnumerable<TItem> source, int size)
		{
			var batch = new List<TItem>(size);
			foreach (var item in source)
			{
				batch.Add(item);
				if (batch.Count == size)
				{
					yield return batch;
					batch = new List<TItem>(size);
				}
			}

			if (batch.Count > 0)
			{
				yield return batch;
			}
		}

		private static (IQuery<T> Query, IReadOnlyCollection<FieldExposer> Fields) Deconstruct(IPartialQuery<T> partialQuery)
		{
			if (partialQuery == null)
			{
				throw new ArgumentNullException(nameof(partialQuery));
			}

			if (partialQuery.Query?.Filter == null)
			{
				throw new ArgumentException("The query and its filter can't be null.", nameof(partialQuery));
			}

			var fields = partialQuery.Fields?.Where(x => x != null).Select(x => x.Exposer).ToList();
			if (fields == null || fields.Count == 0)
			{
				throw new ArgumentException("At least one field must be selected.", nameof(partialQuery));
			}

			return (partialQuery.Query, fields);
		}

		private IEnumerable<PartialObject<T>> Read(IQuery<T> query, IReadOnlyCollection<FieldExposer> fields)
		{
			// Validates the fields before anything is read, so an unsupported field fails immediately instead of on enumeration.
			var selectedFields = fieldMapper.CreateSelectedFields(fields);

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<PartialObject<T>>();
			}

			return ActivityHelper.Track(activityName, "ReadPartial", act =>
			{
				act?.AddTag("Fields", String.Join(",", fields.Select(x => x.fieldName)));

				var domQuery = TranslateQuery(query);

				IEnumerable<PartialObject<T>> Iterator()
				{
					var domPartialObjects = InstanceFactory.ReadPartialInstances(getDomHelper(), domQuery, selectedFields);

					foreach (var batch in Batch(domPartialObjects, BatchSize))
					{
						foreach (var partialObject in fieldMapper.CreatePartialObjects(planApi, batch, fields))
						{
							yield return partialObject;
						}
					}
				}

				return Iterator();
			});
		}

		private IEnumerable<IPagedResult<PartialObject<T>>> ReadPaged(IQuery<T> query, IReadOnlyCollection<FieldExposer> fields, int pageSize)
		{
			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			var selectedFields = fieldMapper.CreateSelectedFields(fields);

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<IPagedResult<PartialObject<T>>>();
			}

			return ReadPagedIterator(TranslateQuery(query), selectedFields, fields, pageSize);
		}

		private IEnumerable<IPagedResult<PartialObject<T>>> ReadPagedIterator(IQuery<DomInstance> domQuery, SelectedFields<DomInstance> selectedFields, IReadOnlyCollection<FieldExposer> fields, int pageSize)
		{
			var pageNumber = 0;
			var pages = InstanceFactory.ReadPartialInstancesPaged(getDomHelper(), domQuery, selectedFields, pageSize);
			var enumerator = pages.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();

				var partialObjects = Batch(page, BatchSize)
					.SelectMany(batch => fieldMapper.CreatePartialObjects(planApi, batch, fields))
					.ToList();

				yield return new PagedResult<PartialObject<T>>(partialObjects, pageNumber++, pageSize, hasNext);
			}
		}

		private IQuery<DomInstance> TranslateQuery(IQuery<T> query)
		{
			return query
				.WithFilter(filterTranslator.TranslateFilter(query.Filter))
				.WithOrder(filterTranslator.TranslateFullOrderBy(query.Order))
				.WithLimit(query.Limit);
		}
	}
}
