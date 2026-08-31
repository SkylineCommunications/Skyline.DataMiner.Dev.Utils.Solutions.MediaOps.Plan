namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using SLDataGateway.API.Types.Querying;

	/// <summary>
	/// Provides repository operations for managing <see cref="RelationshipObjectType"/> objects.
	/// </summary>
	internal class RelationshipObjectTypesRepository : Repository, IRelationshipObjectTypesRepository
	{
		private readonly RelationshipObjectTypeFilterTranslator filterTranslator = new RelationshipObjectTypeFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipObjectTypesRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public RelationshipObjectTypesRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		/// <summary>
		/// Gets the total number of relationship object types in the repository.
		/// </summary>
		/// <returns>The total count of relationship object types.</returns>
		public long Count()
		{
			return Count(new TRUEFilterElement<RelationshipObjectType>());
		}

		/// <summary>
		/// Gets the number of relationship object types that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when counting relationship object types.</param>
		/// <returns>The count of relationship object types matching the filter.</returns>
		public long Count(FilterElement<RelationshipObjectType> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return 0;
			}

			return PlanApi.DomHelpers.SlcRelationshipsHelper.CountRelationshipsInstances(filterTranslator.TranslateFilter(filter));
		}

		/// <summary>
		/// Gets the number of relationship object types that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when counting relationship object types.</param>
		/// <returns>The count of relationship object types matching the query.</returns>
		public long Count(IQuery<RelationshipObjectType> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return 0;
			}

			return PlanApi.DomHelpers.SlcRelationshipsHelper.CountRelationshipsInstances(TranslateToDomQuery(query));
		}

		/// <summary>
		/// Creates a new relationship object type in the repository.
		/// </summary>
		/// <param name="apiObject">The relationship object type to create.</param>
		/// <returns>The created relationship object type.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create an existing relationship object type.</exception>
		/// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified relationship object type.</exception>
		public RelationshipObjectType Create(RelationshipObjectType apiObject)
		{
			PlanApi.Logger.Information(this, "Creating new relationship object type...");

			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Create), act =>
			{
				if (!apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Create for existing relationship object type. Use CreateOrUpdate or Update instead.");
				}

				if (!DomRelationshipObjectTypeHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("RelationshipObjectTypeId", result.SuccessfulIds.Single());

				return new RelationshipObjectType(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Creates multiple new relationship object types in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationship object types to create.</param>
		/// <returns>A read-only collection containing the created relationship object types.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create existing relationship object types.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more relationship object types.</exception>
		public IReadOnlyCollection<RelationshipObjectType> Create(IEnumerable<RelationshipObjectType> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Create), act =>
			{
				if (list.Any(x => !x.IsNew))
				{
					throw new InvalidOperationException("Not possible to use method Create for existing relationship object types. Use CreateOrUpdate or Update instead.");
				}

				if (!DomRelationshipObjectTypeHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("RelationshipObjectTypeIds", String.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new RelationshipObjectType(x)).ToList();
			});
		}

		/// <summary>
		/// Creates new relationship object types or updates existing ones in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationship object types to create or update.</param>
		/// <returns>A read-only collection containing the created or updated relationship object types.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more relationship object types.</exception>
		public IReadOnlyCollection<RelationshipObjectType> CreateOrUpdate(IEnumerable<RelationshipObjectType> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(CreateOrUpdate), act =>
			{
				if (!DomRelationshipObjectTypeHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Created or Updated Relationship Object Types", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Created or Updated Relationship Object Types Count", result.SuccessfulIds.Count);

				return result.SuccessfulItems.Select(x => new RelationshipObjectType(x)).ToList();
			});
		}

		/// <summary>
		/// Deletes the specified relationship object types from the repository.
		/// </summary>
		/// <param name="apiObjects">The relationship object types to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		public void Delete(IEnumerable<RelationshipObjectType> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			Delete(apiObjects.Select(x => x.Id).ToArray());
		}

		/// <summary>
		/// Deletes relationship object types with the specified identifiers from the repository.
		/// </summary>
		/// <param name="apiObjectIds">The unique identifiers of the relationship object types to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more relationship object types.</exception>
		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var toDelete = Read(apiObjectIds.ToArray());

			ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Delete), act =>
			{
				if (!DomRelationshipObjectTypeHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Removed Relationship Object Types", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Removed Relationship Object Types Count", result.SuccessfulIds.Count);
			});
		}

		/// <summary>
		/// Deletes the specified relationship object type from the repository.
		/// </summary>
		/// <param name="oToDelete">The relationship object type to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified relationship object type.</exception>
		public void Delete(RelationshipObjectType oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		/// <summary>
		/// Deletes the specified relationship object type from the repository.
		/// </summary>
		/// <param name="apiObjectId">The unique identifier of the relationship object type to delete.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified relationship object type.</exception>
		public void Delete(Guid apiObjectId)
		{
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Delete), act =>
			{
				if (!DomRelationshipObjectTypeHandler.TryDelete(PlanApi, [toDelete], out var result))
				{
					result.ThrowSingleException(toDelete.Id);
				}

				act?.AddTag("RelationshipObjectTypeId", result.SuccessfulIds.First());
			});
		}

		/// <summary>
		/// Reads a single relationship object type by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the relationship object type.</param>
		/// <returns>The relationship object type with the specified identifier, or <c>null</c> if not found.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public RelationshipObjectType Read(Guid id)
		{
			PlanApi.Logger.Information(this, $"Reading relationship object type with ID: {id}...");

			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Read), act =>
			{
				act?.AddTag("RelationshipObjectTypeId", id);
				var objectType = Read(RelationshipObjectTypeExposers.Id.Equal(id)).FirstOrDefault();

				act?.AddTag("Hit", objectType != null);

				return objectType;
			});
		}

		/// <summary>
		/// Reads multiple relationship object types by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of relationship object types matching the specified identifiers.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
		public IEnumerable<RelationshipObjectType> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<RelationshipObjectType>();
			}

			return Read(new ORFilterElement<RelationshipObjectType>(ids.Select(x => RelationshipObjectTypeExposers.Id.Equal(x)).ToArray()));
		}

		/// <summary>
		/// Reads all relationship object types from the repository.
		/// </summary>
		/// <returns>An enumerable collection of all relationship object types.</returns>
		public IEnumerable<RelationshipObjectType> Read()
		{
			return Read(new TRUEFilterElement<RelationshipObjectType>());
		}

		/// <summary>
		/// Reads relationship object types that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationship object types.</param>
		/// <returns>An enumerable collection of relationship object types matching the filter.</returns>
		public IEnumerable<RelationshipObjectType> Read(FilterElement<RelationshipObjectType> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<RelationshipObjectType>();
			}

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Read), act =>
			{
				var instances = PlanApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(filterTranslator.TranslateFilter(filter));
				return instances.Select(x => new RelationshipObjectType(x));
			});
		}

		/// <summary>
		/// Reads relationship object types that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationship object types.</param>
		/// <returns>An enumerable collection of relationship object types matching the query.</returns>
		public IEnumerable<RelationshipObjectType> Read(IQuery<RelationshipObjectType> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<RelationshipObjectType>();
			}

			var instances = PlanApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(TranslateToDomQuery(query));
			return instances.Select(x => new RelationshipObjectType(x));
		}

		/// <summary>
		/// Reads all relationship object types in pages.
		/// </summary>
		/// <returns>An enumerable collection of pages, where each page contains a collection of relationship object types.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<RelationshipObjectType>());
		}

		/// <summary>
		/// Reads relationship object types that match the specified filter in pages.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationship object types.</param>
		/// <returns>An enumerable collection of pages, where each page contains relationship object types matching the filter.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged(FilterElement<RelationshipObjectType> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads relationship object types that match the specified query in pages.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationship object types.</param>
		/// <returns>An enumerable collection of pages, where each page contains relationship object types matching the query.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged(IQuery<RelationshipObjectType> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ReadPaged(query, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads relationship object types that match the specified filter in pages with a custom page size.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationship object types.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of relationship object types matching the filter.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged(FilterElement<RelationshipObjectType> filter, int pageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			return ReadPagedIterator(filter, pageSize);
		}

		/// <summary>
		/// Reads relationship object types that match the specified query in pages with a custom page size.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationship object types.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of relationship object types matching the query.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged(IQuery<RelationshipObjectType> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<IPagedResult<RelationshipObjectType>>();
			}

			return ReadPagedIterator(query, pageSize);
		}

		/// <summary>
		/// Reads all relationship object types in pages.
		/// </summary>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains a collection of relationship object types.</returns>
		public IEnumerable<IPagedResult<RelationshipObjectType>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<RelationshipObjectType>(), pageSize);
		}

		/// <summary>
		/// Updates an existing relationship object type in the repository.
		/// </summary>
		/// <param name="apiObject">The relationship object type to update.</param>
		/// <returns>The updated relationship object type.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update a new relationship object type that doesn't exist yet.</exception>
		/// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified relationship object type.</exception>
		public RelationshipObjectType Update(RelationshipObjectType apiObject)
		{
			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			PlanApi.Logger.Information(this, $"Updating existing relationship object type {apiObject.Name}...");

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Update), act =>
			{
				if (apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Update for new relationship object type. Use Create or CreateOrUpdate instead.");
				}

				if (!DomRelationshipObjectTypeHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("RelationshipObjectTypeId", result.SuccessfulIds.Single());

				return new RelationshipObjectType(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Updates multiple existing relationship object types in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationship object types to update.</param>
		/// <returns>A read-only collection containing the updated relationship object types.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update new relationship object types that don't exist yet.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more relationship object types.</exception>
		public IReadOnlyCollection<RelationshipObjectType> Update(IEnumerable<RelationshipObjectType> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipObjectTypesRepository), nameof(Update), act =>
			{
				if (list.Any(x => x.IsNew))
				{
					throw new InvalidOperationException("Not possible to use method Update for new relationship object types. Use Create or CreateOrUpdate instead.");
				}

				if (!DomRelationshipObjectTypeHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("RelationshipObjectTypeIds", String.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new RelationshipObjectType(x)).ToList();
			});
		}

		private IEnumerable<IPagedResult<RelationshipObjectType>> ReadPagedIterator(FilterElement<RelationshipObjectType> filter, int pageSize)
		{
			var pageNumber = 0;
			var paramFilter = filterTranslator.TranslateFilter(filter);
			var items = PlanApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypesPaged(paramFilter, pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<RelationshipObjectType>(page.Select(x => new RelationshipObjectType(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IEnumerable<IPagedResult<RelationshipObjectType>> ReadPagedIterator(IQuery<RelationshipObjectType> query, int pageSize)
		{
			var pageNumber = 0;
			var items = PlanApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypesPaged(TranslateToDomQuery(query), pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<RelationshipObjectType>(page.Select(x => new RelationshipObjectType(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IQuery<DomInstance> TranslateToDomQuery(IQuery<RelationshipObjectType> query)
		{
			var domFilter = filterTranslator.TranslateFilter(query.Filter);
			var domOrderBy = filterTranslator.TranslateFullOrderBy(query.Order);

			return query
				.WithFilter(domFilter)
				.WithOrder(domOrderBy)
				.WithLimit(query.Limit);
		}
	}
}
