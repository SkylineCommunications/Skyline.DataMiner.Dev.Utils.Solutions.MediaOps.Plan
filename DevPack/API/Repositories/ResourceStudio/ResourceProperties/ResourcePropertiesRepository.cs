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
	/// Provides repository operations for managing <see cref="ResourceProperty"/> objects.
	/// </summary>
	internal class ResourcePropertiesRepository : Repository, IResourcePropertiesRepository
	{
		private readonly ResourcePropertyFilterTranslator filterTranslator = new ResourcePropertyFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePropertiesRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public ResourcePropertiesRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		/// <summary>
		/// Gets the total number of resource properties in the repository.
		/// </summary>
		/// <returns>The total count of resource properties.</returns>
		public long Count()
		{
			return Count(new TRUEFilterElement<ResourceProperty>());
		}

		/// <summary>
		/// Gets the number of resource properties that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when counting resource properties.</param>
		/// <returns>The count of resource properties matching the filter.</returns>
		public long Count(FilterElement<ResourceProperty> filter)
		{
			return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(filterTranslator.Translate(filter));
		}

		/// <summary>
		/// Gets the number of resource properties that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when counting resource properties.</param>
		/// <returns>The count of resource properties matching the query.</returns>
		public long Count(IQuery<ResourceProperty> query)
		{
			return Count(query.Filter);
		}

		/// <summary>
		/// Creates a new resource property in the repository.
		/// </summary>
		/// <param name="apiObject">The resource property to create.</param>
		/// <returns>The created resource property.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create an existing resource property.</exception>
		/// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified resource property.</exception>
		public ResourceProperty Create(ResourceProperty apiObject)
		{
			PlanApi.Logger.Information(this, "Creating new ResourceProperty...");

			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
			{
				if (!apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Create for existing resource property. Use CreateOrUpdate or Update instead.");
				}

				if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("ResourcePropertyId", result.SuccessfulIds.Single());

				return new ResourceProperty(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Creates multiple new resource properties in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of resource properties to create.</param>
		/// <returns>A read-only collection containing the created resource properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create existing resource properties.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more resource properties.</exception>
		public IReadOnlyCollection<ResourceProperty> Create(IEnumerable<ResourceProperty> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
			{
				var existingProperties = list.Where(x => !x.IsNew);
				if (existingProperties.Any())
				{
					throw new InvalidOperationException("Not possible to use method Create for existing resource properties. Use CreateOrUpdate or Update instead.");
				}

				if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("ResourcePropertyIds", string.Join(", ", result.SuccessfulIds));

				return result.SuccessfulItems.Select(x => new ResourceProperty(x)).ToList();
			});
		}

		/// <summary>
		/// Creates new resource properties or updates existing ones in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of resource properties to create or update.</param>
		/// <returns>A read-only collection containing the created or updated resource properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more resource properties.</exception>
		public IReadOnlyCollection<ResourceProperty> CreateOrUpdate(IEnumerable<ResourceProperty> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(CreateOrUpdate), act =>
			{
				if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Created or Updated Resource Properties", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Created or Updated Resource Properties Count", result.SuccessfulIds.Count);

				return result.SuccessfulItems.Select(x => new ResourceProperty(x)).ToList();
			});
		}

		/// <summary>
		/// Deletes the specified resource properties from the repository.
		/// </summary>
		/// <param name="apiObjects">The resource properties to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		public void Delete(IEnumerable<ResourceProperty> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			Delete(apiObjects.Select(x => x.Id).ToArray());
		}

		/// <summary>
		/// Deletes resource properties with the specified identifiers from the repository.
		/// </summary>
		/// <param name="apiObjectIds">The unique identifiers of the resource properties to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more resource properties.</exception>
		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var propertiesToDelete = Read(apiObjectIds.ToArray());

			ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Delete), act =>
			{
				if (!DomResourcePropertyHandler.TryDelete(PlanApi, propertiesToDelete?.ToList(), out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Removed Resource Properties", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Removed Resource Properties Count", result.SuccessfulIds.Count);
			});
		}

		/// <summary>
		/// Deletes the specified resource property from the repository.
		/// </summary>
		/// <param name="oToDelete">The resource property to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified resource property.</exception>
		public void Delete(ResourceProperty oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		/// <summary>
		/// Deletes the specified resource property from the repository.
		/// </summary>
		/// <param name="apiObjectId">The unique identifier of the resource property to delete.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified resource property.</exception>
		public void Delete(Guid apiObjectId)
		{
			var propertyToDelete = Read(apiObjectId);
			if (propertyToDelete == null)
			{
				return;
			}

			ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Delete), act =>
			{
				if (!DomResourcePropertyHandler.TryDelete(PlanApi, [propertyToDelete], out var result))
				{
					result.ThrowSingleException(propertyToDelete.Id);
				}

				act?.AddTag("ResourcePropertyId", result.SuccessfulIds.First());
			});
		}

		/// <summary>
		/// Reads a single resource property by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the resource property.</param>
		/// <returns>The resource property with the specified identifier, or <c>null</c> if not found.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public ResourceProperty Read(Guid id)
		{
			PlanApi.Logger.Information(this, $"Reading Resource Property with ID: {id}...");

			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
			{
				act?.AddTag("ResourcePropertyId", id);
				var property = Read(ResourcePropertyExposers.Id.Equal(id)).FirstOrDefault();

				if (property == null)
				{
					act?.AddTag("Hit", false);
					return null;
				}

				act?.AddTag("Hit", true);

				return property;
			});
		}

		/// <summary>
		/// Reads multiple resource properties by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of resource properties matching the specified identifiers.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
		public IEnumerable<ResourceProperty> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<ResourceProperty>();
			}

			return Read(new ORFilterElement<ResourceProperty>(ids.Select(x => ResourcePropertyExposers.Id.Equal(x)).ToArray()));
		}

		/// <summary>
		/// Reads all resource properties from the repository.
		/// </summary>
		/// <returns>An enumerable collection of all resource properties.</returns>
		public IEnumerable<ResourceProperty> Read()
		{
			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
			{
				return Read(new TRUEFilterElement<ResourceProperty>());
			});
		}

		/// <summary>
		/// Reads resource properties that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading resource properties.</param>
		/// <returns>An enumerable collection of resource properties matching the filter.</returns>
		public IEnumerable<ResourceProperty> Read(FilterElement<ResourceProperty> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
			{
				var properties = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(filterTranslator.Translate(filter));
				return properties.Select(x => new ResourceProperty(x));
			});
		}

		/// <summary>
		/// Reads resource properties that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading resource properties.</param>
		/// <returns>An enumerable collection of resource properties matching the query.</returns>
		public IEnumerable<ResourceProperty> Read(IQuery<ResourceProperty> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
			{
				return Read(query.Filter);
			});
		}

		/// <summary>
		/// Reads all resource properties in pages.
		/// </summary>
		/// <returns>An enumerable collection of pages, where each page contains a collection of resource properties.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<ResourceProperty>());
		}

		/// <summary>
		/// Reads resource properties that match the specified filter in pages.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading resource properties.</param>
		/// <returns>An enumerable collection of pages, where each page contains resource properties matching the filter.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(FilterElement<ResourceProperty> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads resource properties that match the specified query in pages.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading resource properties.</param>
		/// <returns>An enumerable collection of pages, where each page contains resource properties matching the query.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(IQuery<ResourceProperty> query)
		{
			return ReadPaged(query.Filter);
		}

		/// <summary>
		/// Reads resource properties that match the specified filter in pages with a custom page size.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading resource properties.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource properties matching the filter.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(FilterElement<ResourceProperty> filter, int pageSize)
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
		/// Reads resource properties that match the specified query in pages with a custom page size.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading resource properties.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource properties matching the query.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(IQuery<ResourceProperty> query, int pageSize)
		{
			return ReadPaged(query.Filter, pageSize);
		}

		/// <summary>
		/// Reads all resource properties in pages.
		/// </summary>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains a collection of resource properties.</returns>
		public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<ResourceProperty>(), MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Updates an existing resource property in the repository.
		/// </summary>
		/// <param name="apiObject">The resource property to update.</param>
		/// <returns>The updated resource property.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update a new resource property that doesn't exist yet.</exception>
		/// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified resource property.</exception>
		public ResourceProperty Update(ResourceProperty apiObject)
		{
			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			PlanApi.Logger.Information(this, $"Updating existing Resource Property {apiObject.Name}...");

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Update), act =>
			{
				if (apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Update for new resource property. Use Create or CreateOrUpdate instead.");
				}

				if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("ResourcePropertyId", result.SuccessfulIds.Single());

				return new ResourceProperty(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Updates multiple existing resource properties in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of resource properties to update.</param>
		/// <returns>A read-only collection containing the updated resource properties.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update new resource properties that don't exist yet.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more resource properties.</exception>
		public IReadOnlyCollection<ResourceProperty> Update(IEnumerable<ResourceProperty> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Update), act =>
			{
				var newProperties = list.Where(x => x.IsNew);
				if (newProperties.Any())
				{
					throw new InvalidOperationException("Not possible to use method Update for new resource properties. Use Create or CreateOrUpdate instead.");
				}

				if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				var resourcePropertyIds = result.SuccessfulIds;
				act?.AddTag("ResourcePropertyIds", String.Join(", ", resourcePropertyIds));

				return result.SuccessfulItems.Select(x => new ResourceProperty(x)).ToList();
			});
		}

		private IEnumerable<IPagedResult<ResourceProperty>> ReadPagedIterator(FilterElement<ResourceProperty> filter, int pageSize)
		{
			var pageNumber = 0;
			var paramFilter = filterTranslator.Translate(filter);
			var items = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePropertiesPaged(paramFilter, pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<ResourceProperty>(page.Select(x => new ResourceProperty(x)), pageNumber++, pageSize, hasNext);
			}
		}
	}
}
