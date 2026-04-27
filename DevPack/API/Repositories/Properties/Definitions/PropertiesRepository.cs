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
	/// Provides repository operations for managing <see cref="Property"/> objects.
	/// </summary>
	internal class PropertiesRepository : Repository, IPropertiesRepository
	{
		private readonly PropertyFilterTranslator filterTranslator = new PropertyFilterTranslator();

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertiesRepository"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API instance.</param>
		public PropertiesRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		/// <summary>
		/// Gets the total number of property definitions in the repository.
		/// </summary>
		/// <returns>The total count of property definitions.</returns>
		public long Count()
		{
			return Count(new TRUEFilterElement<Property>());
		}

		/// <summary>
		/// Gets the number of property definitions that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when counting property definitions.</param>
		/// <returns>The count of property definitions matching the filter.</returns>
		public long Count(FilterElement<Property> filter)
		{
			return PlanApi.DomHelpers.SlcPropertiesHelper.CountPropertiesInstances(filterTranslator.Translate(filter));
		}

		/// <summary>
		/// Gets the number of property definitions that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when counting property definitions.</param>
		/// <returns>The count of property definitions matching the query.</returns>
		public long Count(IQuery<Property> query)
		{
			return Count(query.Filter);
		}

		/// <summary>
		/// Creates a new property definition in the repository.
		/// </summary>
		/// <param name="apiObject">The property definition to create.</param>
		/// <returns>The created property definition.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create an existing property definition.</exception>
		/// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified property definition.</exception>
		public Property Create(Property apiObject)
		{
			PlanApi.Logger.Information(this, "Creating new Property definition...");

			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Create), act =>
			{
				if (!apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Create for existing property definition. Use CreateOrUpdate or Update instead.");
				}

				if (!DomPropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("PropertyId", result.SuccessfulIds.Single());

				return Property.InstantiateProperty(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Creates multiple new property definitions in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property definitions to create.</param>
		/// <returns>A read-only collection containing the created property definitions.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to create existing property definitions.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more property definitions.</exception>
		public IReadOnlyCollection<Property> Create(IEnumerable<Property> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Create), act =>
			{
				var existingProperties = list.Where(x => !x.IsNew);
				if (existingProperties.Any())
				{
					throw new InvalidOperationException("Not possible to use method Create for existing property definitions. Use CreateOrUpdate or Update instead.");
				}

				if (!DomPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("PropertyIds", string.Join(", ", result.SuccessfulIds));

				return Property.InstantiateProperties(result.SuccessfulItems).ToList();
			});
		}

		/// <summary>
		/// Creates new property definitions or updates existing ones in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property definitions to create or update.</param>
		/// <returns>A read-only collection containing the created or updated property definitions.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more property definitions.</exception>
		public IReadOnlyCollection<Property> CreateOrUpdate(IEnumerable<Property> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(CreateOrUpdate), act =>
			{
				if (!DomPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Created or Updated Property Definitions", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Created or Updated Property Definitions Count", result.SuccessfulIds.Count);

				return Property.InstantiateProperties(result.SuccessfulItems).ToList();
			});
		}

		/// <summary>
		/// Deletes the specified property definitions from the repository.
		/// </summary>
		/// <param name="apiObjects">The property definitions to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		public void Delete(IEnumerable<Property> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			Delete(apiObjects.Select(x => x.Id).ToArray());
		}

		/// <summary>
		/// Deletes property definitions with the specified identifiers from the repository.
		/// </summary>
		/// <param name="apiObjectIds">The unique identifiers of the property definitions to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more property definitions.</exception>
		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var propertiesToDelete = Read(apiObjectIds.ToArray());

			ActivityHelper.Track(nameof(PropertiesRepository), nameof(Delete), act =>
			{
				if (!DomPropertyHandler.TryDelete(PlanApi, propertiesToDelete?.ToList(), out var result))
				{
					result.ThrowBulkException();
				}

				act?.AddTag("Removed Property Definitions", String.Join(", ", result.SuccessfulIds));
				act?.AddTag("Removed Property Definitions Count", result.SuccessfulIds.Count);
			});
		}

		/// <summary>
		/// Deletes the specified property definition from the repository.
		/// </summary>
		/// <param name="oToDelete">The property definition to delete.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property definition.</exception>
		public void Delete(Property oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		/// <summary>
		/// Deletes the specified property definition from the repository.
		/// </summary>
		/// <param name="apiObjectId">The unique identifier of the property definition to delete.</param>
		/// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified property definition.</exception>
		public void Delete(Guid apiObjectId)
		{
			var propertyToDelete = Read(apiObjectId);
			if (propertyToDelete == null)
			{
				return;
			}

			ActivityHelper.Track(nameof(PropertiesRepository), nameof(Delete), act =>
			{
				if (!DomPropertyHandler.TryDelete(PlanApi, [propertyToDelete], out var result))
				{
					result.ThrowSingleException(propertyToDelete.Id);
				}

				act?.AddTag("PropertyId", result.SuccessfulIds.First());
			});
		}

		/// <summary>
		/// Reads a single property definition by its unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier of the property definition.</param>
		/// <returns>The property definition with the specified identifier, or <c>null</c> if not found.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
		public Property Read(Guid id)
		{
			PlanApi.Logger.Information(this, $"Reading Property definition with ID: {id}...");

			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Read), act =>
			{
				act?.AddTag("PropertyId", id);
				var property = Read(PropertyExposers.Id.Equal(id)).FirstOrDefault();

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
		/// Reads multiple property definitions by their unique identifiers.
		/// </summary>
		/// <param name="ids">A collection of unique identifiers.</param>
		/// <returns>An enumerable collection of property definitions matching the specified identifiers.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
		public IEnumerable<Property> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Array.Empty<Property>();
			}

			return Read(new ORFilterElement<Property>(ids.Select(x => PropertyExposers.Id.Equal(x)).ToArray()));
		}

		/// <summary>
		/// Reads all property definitions from the repository.
		/// </summary>
		/// <returns>An enumerable collection of all property definitions.</returns>
		public IEnumerable<Property> Read()
		{
			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Read), act =>
			{
				return Read(new TRUEFilterElement<Property>());
			});
		}

		/// <summary>
		/// Reads property definitions that match the specified filter.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property definitions.</param>
		/// <returns>An enumerable collection of property definitions matching the filter.</returns>
		public IEnumerable<Property> Read(FilterElement<Property> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Read), act =>
			{
				var properties = PlanApi.DomHelpers.SlcPropertiesHelper.GetProperties(filterTranslator.Translate(filter));
				return Property.InstantiateProperties(properties);
			});
		}

		/// <summary>
		/// Reads property definitions that match the specified query.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property definitions.</param>
		/// <returns>An enumerable collection of property definitions matching the query.</returns>
		public IEnumerable<Property> Read(IQuery<Property> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Read), act =>
			{
				return Read(query.Filter);
			});
		}

		/// <summary>
		/// Reads all property definitions in pages.
		/// </summary>
		/// <returns>An enumerable collection of pages, where each page contains a collection of property definitions.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<Property>());
		}

		/// <summary>
		/// Reads property definitions that match the specified filter in pages.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property definitions.</param>
		/// <returns>An enumerable collection of pages, where each page contains property definitions matching the filter.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged(FilterElement<Property> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		/// <summary>
		/// Reads property definitions that match the specified query in pages.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property definitions.</param>
		/// <returns>An enumerable collection of pages, where each page contains property definitions matching the query.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged(IQuery<Property> query)
		{
			return ReadPaged(query.Filter);
		}

		/// <summary>
		/// Reads property definitions that match the specified filter in pages with a custom page size.
		/// </summary>
		/// <param name="filter">The filter criteria to apply when reading property definitions.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of property definitions matching the filter.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged(FilterElement<Property> filter, int pageSize)
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
		/// Reads property definitions that match the specified query in pages with a custom page size.
		/// </summary>
		/// <param name="query">The query criteria to apply when reading property definitions.</param>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains up to the specified number of property definitions matching the query.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged(IQuery<Property> query, int pageSize)
		{
			return ReadPaged(query.Filter, pageSize);
		}

		/// <summary>
		/// Reads all property definitions in pages.
		/// </summary>
		/// <param name="pageSize">The number of items per page.</param>
		/// <returns>An enumerable collection of pages, where each page contains a collection of property definitions.</returns>
		public IEnumerable<IPagedResult<Property>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<Property>(), pageSize);
		}

		/// <summary>
		/// Updates an existing property definition in the repository.
		/// </summary>
		/// <param name="apiObject">The property definition to update.</param>
		/// <returns>The updated property definition.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update a new property definition that doesn't exist yet.</exception>
		/// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified property definition.</exception>
		public Property Update(Property apiObject)
		{
			if (apiObject == null)
			{
				throw new ArgumentNullException(nameof(apiObject));
			}

			PlanApi.Logger.Information(this, $"Updating existing Property definition {apiObject.Name}...");

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Update), act =>
			{
				if (apiObject.IsNew)
				{
					throw new InvalidOperationException("Not possible to use method Update for new property definition. Use Create or CreateOrUpdate instead.");
				}

				if (!DomPropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
				{
					result.ThrowSingleException(apiObject.Id);
				}

				act?.AddTag("PropertyId", result.SuccessfulIds.Single());

				return Property.InstantiateProperty(result.SuccessfulItems.Single());
			});
		}

		/// <summary>
		/// Updates multiple existing property definitions in the repository.
		/// </summary>
		/// <param name="apiObjects">The collection of property definitions to update.</param>
		/// <returns>A read-only collection containing the updated property definitions.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when attempting to update new property definitions that don't exist yet.</exception>
		/// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more property definitions.</exception>
		public IReadOnlyCollection<Property> Update(IEnumerable<Property> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var list = apiObjects.ToList();

			return ActivityHelper.Track(nameof(PropertiesRepository), nameof(Update), act =>
			{
				var newProperties = list.Where(x => x.IsNew);
				if (newProperties.Any())
				{
					throw new InvalidOperationException("Not possible to use method Update for new property definitions. Use Create or CreateOrUpdate instead.");
				}

				if (!DomPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
				{
					result.ThrowBulkException();
				}

				var propertyIds = result.SuccessfulIds;
				act?.AddTag("PropertyIds", String.Join(", ", propertyIds));

				return Property.InstantiateProperties(result.SuccessfulItems).ToList();
			});
		}

		private IEnumerable<IPagedResult<Property>> ReadPagedIterator(FilterElement<Property> filter, int pageSize)
		{
			var pageNumber = 0;
			var paramFilter = filterTranslator.Translate(filter);
			var items = PlanApi.DomHelpers.SlcPropertiesHelper.GetPropertiesPaged(paramFilter, pageSize);
			var enumerator = items.GetEnumerator();
			var hasNext = enumerator.MoveNext();

			while (hasNext)
			{
				var page = enumerator.Current;
				hasNext = enumerator.MoveNext();
				yield return new PagedResult<Property>(Property.InstantiateProperties(page), pageNumber++, pageSize, hasNext);
			}
		}
	}
}

