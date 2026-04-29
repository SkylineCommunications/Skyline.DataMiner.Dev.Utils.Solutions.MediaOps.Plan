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
	/// Provides repository operations for managing <see cref="PropertyValueCollection"/> objects.
	/// </summary>
	internal class PropertyValueCollectionsRepository : Repository, IPropertyValueCollectionsRepository
	{
		private readonly PropertyValueCollectionFilterTranslator filterTranslator = new PropertyValueCollectionFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValueCollectionsRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public PropertyValueCollectionsRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		/// <summary>
		/// Gets the total number of property collections in the repository.
		/// </summary>
		/// <returns>The total count of property collections.</returns>
		public long Count()
		{
			return Count(new TRUEFilterElement<PropertyValueCollection>());
		}

		/// <summary>
		/// Gets the number of property collections that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when counting property collections.</param>
		/// <returns>The count of property collections matching the filter.</returns>
		public long Count(FilterElement<PropertyValueCollection> filter)
		{
			return PlanApi.DomHelpers.SlcPropertiesHelper.CountPropertiesInstances(filterTranslator.Translate(filter));
		}

		/// <summary>
		/// Gets the number of property collections that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when counting property collections.</param>
		/// <returns>The count of property collections matching the query.</returns>
		public long Count(IQuery<PropertyValueCollection> query)
		{
			return Count(query.Filter);
		}

		/// <summary>
		/// Creates a new property collection in the repository.
		/// </summary>
		/// <param name="apiObject">The property collection to create.</param>
		/// <returns>The created property collection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create an existing property collection.</exception>
		/// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified property collection.</exception>
		public PropertyValueCollection Create(PropertyValueCollection apiObject)
		{
			PlanApi.Logger.Information(this, "Creating new PropertyCollection...");

			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Create), act =>
			{
				if (!apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Create for existing property collection. Use CreateOrUpdate or Update instead.");
				}

				if (!DomPropertyValueCollectionHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("PropertyCollectionId", result.SuccessfulIds.Single());

				return new SlcPropertyValueCollection(PlanApi, result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Creates multiple new property collections in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property collections to create.</param>
		/// <returns>A read-only collection containing the created property collections.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create existing property collections.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more property collections.</exception>
		public IReadOnlyCollection<PropertyValueCollection> Create(IEnumerable<PropertyValueCollection> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Create), act =>
			{
				var existingCollections = list.Where(x => !x.IsNew);
				if (existingCollections.Any())
				{
					throw new InvalidOperationException("Not possible to use method Create for existing property collections. Use CreateOrUpdate or Update instead.");
				}

				if (!DomPropertyValueCollectionHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("PropertyCollectionIds", string.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new SlcPropertyValueCollection(PlanApi, x)).ToList();
			});
		}

		/// <summary>
		/// Creates new property collections or updates existing ones in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property collections to create or update.</param>
		/// <returns>A read-only collection containing the created or updated property collections.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more property collections.</exception>
		public IReadOnlyCollection<PropertyValueCollection> CreateOrUpdate(IEnumerable<PropertyValueCollection> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(CreateOrUpdate), act =>
			{
				if (!DomPropertyValueCollectionHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Created or Updated Property Collections", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Created or Updated Property Collections Count", result.SuccessfulIds.Count);

				return result.SuccessfulItems.Select(x => new SlcPropertyValueCollection(PlanApi, x)).ToList();
			});
		}

		/// <summary>
		/// Deletes the specified property collections from the repository.
		/// </summary>
		/// <param name="apiObjects">The property collections to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		public void Delete(IEnumerable<PropertyValueCollection> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			Delete(apiObjects.Select(x => x.Id).ToArray());
		}

		/// <summary>
		/// Deletes property collections with the specified identifiers from the repository.
		/// </summary>
		/// <param name="apiObjectIds">The unique identifiers of the property collections to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more property collections.</exception>
		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var collectionsToDelete = Read(apiObjectIds.ToArray());

			ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Delete), act =>
			{
				if (!DomPropertyValueCollectionHandler.TryDelete(PlanApi, collectionsToDelete?.ToList(), out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Removed Property Collections", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Removed Property Collections Count", result.SuccessfulIds.Count);
			});
		}

		/// <summary>
		/// Deletes the specified property collection from the repository.
		/// </summary>
		/// <param name="oToDelete">The property collection to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property collection.</exception>
		public void Delete(PropertyValueCollection oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		/// <summary>
		/// Deletes the specified property collection from the repository.
		/// </summary>
		/// <param name="apiObjectId">The unique identifier of the property collection to delete.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property collection.</exception>
		public void Delete(Guid apiObjectId)
		{
			var collectionToDelete = Read(apiObjectId);
			if (collectionToDelete == null)
			{
				return;
			}

			ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Delete), act =>
			{
				if (!DomPropertyValueCollectionHandler.TryDelete(PlanApi, [collectionToDelete], out var result))
				{
					result.ThrowSingleException(collectionToDelete.Id);
				}

				act?.AddTag("PropertyCollectionId", result.SuccessfulIds.First());
			});
		}

		/// <summary>
		/// Reads a single property collection by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the property collection.</param>
		/// <returns>The property collection with the specified identifier, or <c>null</c> if not found.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public PropertyValueCollection Read(Guid id)
		{
			PlanApi.Logger.Information(this, $"Reading PropertyCollection with ID: {id}...");

			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Read), act =>
			{
				act?.AddTag("PropertyCollectionId", id);
				var collection = Read(PropertyValueCollectionExposers.Id.Equal(id)).FirstOrDefault();

				if (collection == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);

				return collection;
			});
		}

		/// <summary>
		/// Reads multiple property collections by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of property collections matching the specified identifiers.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
		public IEnumerable<PropertyValueCollection> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<PropertyValueCollection>();
			}

			return Read(new ORFilterElement<PropertyValueCollection>(ids.Select(x => PropertyValueCollectionExposers.Id.Equal(x)).ToArray()));
		}

		/// <summary>
		/// Reads all property collections from the repository.
		/// </summary>
		/// <returns>An enumerable collection of all property collections.</returns>
		public IEnumerable<PropertyValueCollection> Read()
		{
			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Read), act =>
			{
				return Read(new TRUEFilterElement<PropertyValueCollection>());
			});
		}

		/// <summary>
		/// Reads property collections that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property collections.</param>
		/// <returns>An enumerable collection of property collections matching the filter.</returns>
		public IEnumerable<PropertyValueCollection> Read(FilterElement<PropertyValueCollection> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Read), act =>
			{
				var instances = PlanApi.DomHelpers.SlcPropertiesHelper.GetPropertyValues(filterTranslator.Translate(filter));
				return instances.Select(x => new SlcPropertyValueCollection(PlanApi, x));
			});
		}

		/// <summary>
		/// Reads property collections that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property collections.</param>
		/// <returns>An enumerable collection of property collections matching the query.</returns>
		public IEnumerable<PropertyValueCollection> Read(IQuery<PropertyValueCollection> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Read), act =>
			{
				return Read(query.Filter);
			});
		}

		/// <summary>
		/// Reads all property collections in pages.
		/// </summary>
		/// <returns>An enumerable collection of pages, where each page contains a collection of property collections.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<PropertyValueCollection>());
		}

		/// <summary>
		/// Reads property collections that match the specified filter in pages.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property collections.</param>
		/// <returns>An enumerable collection of pages, where each page contains property collections matching the filter.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged(FilterElement<PropertyValueCollection> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads property collections that match the specified query in pages.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property collections.</param>
		/// <returns>An enumerable collection of pages, where each page contains property collections matching the query.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged(IQuery<PropertyValueCollection> query)
		{
			return ReadPaged(query.Filter);
		}

		/// <summary>
		/// Reads property collections that match the specified filter in pages with a custom page size.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property collections.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of property collections matching the filter.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged(FilterElement<PropertyValueCollection> filter, int pageSize)
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
		/// Reads property collections that match the specified query in pages with a custom page size.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property collections.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of property collections matching the query.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged(IQuery<PropertyValueCollection> query, int pageSize)
		{
			return ReadPaged(query.Filter, pageSize);
		}

		/// <summary>
		/// Reads all property collections in pages.
		/// </summary>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains a collection of property collections.</returns>
		public IEnumerable<IPagedResult<PropertyValueCollection>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<PropertyValueCollection>(), pageSize);
		}

		/// <summary>
		/// Updates an existing property collection in the repository.
		/// </summary>
		/// <param name="apiObject">The property collection to update.</param>
		/// <returns>The updated property collection.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update a new property collection that doesn't exist yet.</exception>
		/// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified property collection.</exception>
		public PropertyValueCollection Update(PropertyValueCollection apiObject)
		{
			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			PlanApi.Logger.Information(this, $"Updating existing PropertyCollection {apiObject.Id}...");

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Update), act =>
			{
				if (apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Update for new property collection. Use Create or CreateOrUpdate instead.");
				}

				if (!DomPropertyValueCollectionHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("PropertyCollectionId", result.SuccessfulIds.Single());

				return new SlcPropertyValueCollection(PlanApi, result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Updates multiple existing property collections in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property collections to update.</param>
		/// <returns>A read-only collection containing the updated property collections.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update new property collections that don't exist yet.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more property collections.</exception>
		public IReadOnlyCollection<PropertyValueCollection> Update(IEnumerable<PropertyValueCollection> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertyValueCollectionsRepository), nameof(Update), act =>
			{
				var newCollections = list.Where(x => x.IsNew);
				if (newCollections.Any())
				{
					throw new InvalidOperationException("Not possible to use method Update for new property collections. Use Create or CreateOrUpdate instead.");
				}

				if (!DomPropertyValueCollectionHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				var collectionIds = result.SuccessfulIds;
				act?.AddTag("PropertyCollectionIds", String.Join(", ", collectionIds));

				return result.SuccessfulItems.Select(x => new SlcPropertyValueCollection(PlanApi, x)).ToList();
			});
		}

		private IEnumerable<IPagedResult<PropertyValueCollection>> ReadPagedIterator(FilterElement<PropertyValueCollection> filter, int pageSize)
		{
			var pageNumber = 0;
			var paramFilter = filterTranslator.Translate(filter);
			var items = PlanApi.DomHelpers.SlcPropertiesHelper.GetPropertyValuesPaged(paramFilter, pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<PropertyValueCollection>(page.Select(x => new SlcPropertyValueCollection(PlanApi, x)), pageNumber++, pageSize, hasNext);
			}
		}
	}
}

