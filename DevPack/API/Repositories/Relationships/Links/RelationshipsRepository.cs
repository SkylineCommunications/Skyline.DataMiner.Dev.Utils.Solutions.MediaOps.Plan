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
	/// Provides repository operations for managing <see cref="Relationship"/> objects.
	/// </summary>
	internal class RelationshipsRepository : Repository, IRelationshipsRepository
	{
		private readonly RelationshipFilterTranslator filterTranslator = new RelationshipFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipsRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public RelationshipsRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		/// <summary>
		/// Gets the total number of relationships in the repository.
		/// </summary>
		/// <returns>The total count of relationships.</returns>
		public long Count()
		{
			return Count(new TRUEFilterElement<Relationship>());
		}

		/// <summary>
		/// Gets the number of relationships that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when counting relationships.</param>
		/// <returns>The count of relationships matching the filter.</returns>
		public long Count(FilterElement<Relationship> filter)
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
		/// Gets the number of relationships that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when counting relationships.</param>
		/// <returns>The count of relationships matching the query.</returns>
		public long Count(IQuery<Relationship> query)
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
		/// Creates a new relationship in the repository.
		/// </summary>
		/// <param name="apiObject">The relationship to create.</param>
		/// <returns>The created relationship.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create an existing relationship.</exception>
		/// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified relationship.</exception>
		public Relationship Create(Relationship apiObject)
		{
			PlanApi.Logger.Information(this, "Creating new relationship...");

			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Create), act =>
			{
				if (!apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Create for existing relationship. Use CreateOrUpdate or Update instead.");
				}

				if (!DomRelationshipHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("RelationshipId", result.SuccessfulIds.Single());

				return new Relationship(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Creates multiple new relationships in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationships to create.</param>
		/// <returns>A read-only collection containing the created relationships.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create existing relationships.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more relationships.</exception>
		public IReadOnlyCollection<Relationship> Create(IEnumerable<Relationship> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Create), act =>
			{
				if (list.Any(x => !x.IsNew))
				{
					throw new InvalidOperationException("Not possible to use method Create for existing relationships. Use CreateOrUpdate or Update instead.");
				}

				if (!DomRelationshipHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("RelationshipIds", String.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new Relationship(x)).ToList();
			});
		}

		/// <summary>
		/// Creates new relationships or updates existing ones in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationships to create or update.</param>
		/// <returns>A read-only collection containing the created or updated relationships.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more relationships.</exception>
		public IReadOnlyCollection<Relationship> CreateOrUpdate(IEnumerable<Relationship> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(CreateOrUpdate), act =>
			{
				if (!DomRelationshipHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Created or Updated Relationships", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Created or Updated Relationships Count", result.SuccessfulIds.Count);

				return result.SuccessfulItems.Select(x => new Relationship(x)).ToList();
			});
		}

		/// <summary>
		/// Deletes the specified relationships from the repository.
		/// </summary>
		/// <param name="apiObjects">The relationships to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		public void Delete(IEnumerable<Relationship> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			Delete(apiObjects.Select(x => x.Id).ToArray());
		}

		/// <summary>
		/// Deletes relationships with the specified identifiers from the repository.
		/// </summary>
		/// <param name="apiObjectIds">The unique identifiers of the relationships to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more relationships.</exception>
		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var toDelete = Read(apiObjectIds.ToArray());

			ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Delete), act =>
			{
				if (!DomRelationshipHandler.TryDelete(PlanApi, toDelete?.ToList(), out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Removed Relationships", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Removed Relationships Count", result.SuccessfulIds.Count);
			});
		}

		/// <summary>
		/// Deletes the specified relationship from the repository.
		/// </summary>
		/// <param name="oToDelete">The relationship to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified relationship.</exception>
		public void Delete(Relationship oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		/// <summary>
		/// Deletes the specified relationship from the repository.
		/// </summary>
		/// <param name="apiObjectId">The unique identifier of the relationship to delete.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified relationship.</exception>
		public void Delete(Guid apiObjectId)
		{
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Delete), act =>
			{
				if (!DomRelationshipHandler.TryDelete(PlanApi, [toDelete], out var result))
				{
					result.ThrowSingleException(toDelete.Id);
				}

				act?.AddTag("RelationshipId", result.SuccessfulIds.First());
			});
		}

		/// <summary>
		/// Reads a single relationship by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the relationship.</param>
		/// <returns>The relationship with the specified identifier, or <c>null</c> if not found.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public Relationship Read(Guid id)
		{
			PlanApi.Logger.Information(this, $"Reading relationship with ID: {id}...");

			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Read), act =>
			{
				act?.AddTag("RelationshipId", id);
				var relationship = Read(RelationshipExposers.Id.Equal(id)).FirstOrDefault();

				act?.AddTag("Hit", relationship != null);

				return relationship;
			});
		}

		/// <summary>
		/// Reads multiple relationships by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of relationships matching the specified identifiers.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
		public IEnumerable<Relationship> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<Relationship>();
			}

			return Read(new ORFilterElement<Relationship>(ids.Select(x => RelationshipExposers.Id.Equal(x)).ToArray()));
		}

		/// <summary>
		/// Reads all relationships from the repository.
		/// </summary>
		/// <returns>An enumerable collection of all relationships.</returns>
		public IEnumerable<Relationship> Read()
		{
			return Read(new TRUEFilterElement<Relationship>());
		}

		/// <summary>
		/// Reads relationships that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationships.</param>
		/// <returns>An enumerable collection of relationships matching the filter.</returns>
		public IEnumerable<Relationship> Read(FilterElement<Relationship> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<Relationship>();
			}

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Read), act =>
			{
				var instances = PlanApi.DomHelpers.SlcRelationshipsHelper.GetLinks(filterTranslator.TranslateFilter(filter));
				return instances.Select(x => new Relationship(x));
			});
		}

		/// <summary>
		/// Reads relationships that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationships.</param>
		/// <returns>An enumerable collection of relationships matching the query.</returns>
		public IEnumerable<Relationship> Read(IQuery<Relationship> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (query.Filter.isEmpty())
			{
				return Enumerable.Empty<Relationship>();
			}

			var instances = PlanApi.DomHelpers.SlcRelationshipsHelper.GetLinks(TranslateToDomQuery(query));
			return instances.Select(x => new Relationship(x));
		}

		/// <summary>
		/// Reads all relationships in pages.
		/// </summary>
		/// <returns>An enumerable collection of pages, where each page contains a collection of relationships.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<Relationship>());
		}

		/// <summary>
		/// Reads relationships that match the specified filter in pages.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationships.</param>
		/// <returns>An enumerable collection of pages, where each page contains relationships matching the filter.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged(FilterElement<Relationship> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads relationships that match the specified query in pages.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationships.</param>
		/// <returns>An enumerable collection of pages, where each page contains relationships matching the query.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged(IQuery<Relationship> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ReadPaged(query, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads relationships that match the specified filter in pages with a custom page size.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading relationships.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of relationships matching the filter.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged(FilterElement<Relationship> filter, int pageSize)
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
		/// Reads relationships that match the specified query in pages with a custom page size.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading relationships.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of relationships matching the query.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged(IQuery<Relationship> query, int pageSize)
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
				return Enumerable.Empty<IPagedResult<Relationship>>();
			}

			return ReadPagedIterator(query, pageSize);
		}

		/// <summary>
		/// Reads all relationships in pages.
		/// </summary>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains a collection of relationships.</returns>
		public IEnumerable<IPagedResult<Relationship>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<Relationship>(), pageSize);
		}

		/// <summary>
		/// Updates an existing relationship in the repository.
		/// </summary>
		/// <param name="apiObject">The relationship to update.</param>
		/// <returns>The updated relationship.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update a new relationship that doesn't exist yet.</exception>
		/// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified relationship.</exception>
		public Relationship Update(Relationship apiObject)
		{
			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			PlanApi.Logger.Information(this, $"Updating existing relationship {apiObject.Id}...");

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Update), act =>
			{
				if (apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Update for new relationship. Use Create or CreateOrUpdate instead.");
				}

				if (!DomRelationshipHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("RelationshipId", result.SuccessfulIds.Single());

				return new Relationship(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Updates multiple existing relationships in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of relationships to update.</param>
		/// <returns>A read-only collection containing the updated relationships.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update new relationships that don't exist yet.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more relationships.</exception>
		public IReadOnlyCollection<Relationship> Update(IEnumerable<Relationship> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(RelationshipsRepository), nameof(Update), act =>
			{
				if (list.Any(x => x.IsNew))
				{
					throw new InvalidOperationException("Not possible to use method Update for new relationships. Use Create or CreateOrUpdate instead.");
				}

				if (!DomRelationshipHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("RelationshipIds", String.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new Relationship(x)).ToList();
			});
		}

		private IEnumerable<IPagedResult<Relationship>> ReadPagedIterator(FilterElement<Relationship> filter, int pageSize)
		{
			var pageNumber = 0;
			var paramFilter = filterTranslator.TranslateFilter(filter);
			var items = PlanApi.DomHelpers.SlcRelationshipsHelper.GetLinksPaged(paramFilter, pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<Relationship>(page.Select(x => new Relationship(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IEnumerable<IPagedResult<Relationship>> ReadPagedIterator(IQuery<Relationship> query, int pageSize)
		{
			var pageNumber = 0;
			var items = PlanApi.DomHelpers.SlcRelationshipsHelper.GetLinksPaged(TranslateToDomQuery(query), pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<Relationship>(page.Select(x => new Relationship(x)), pageNumber++, pageSize, hasNext);
			}
		}

		private IQuery<DomInstance> TranslateToDomQuery(IQuery<Relationship> query)
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
