namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;

	using SLDataGateway.API.Types.Querying;

	internal class SchedulingPropertiesRepository : Repository, ISchedulingPropertiesRepository
	{
		private readonly SchedulingPropertyFilterTranslator filterTranslator = new SchedulingPropertyFilterTranslator();

		public SchedulingPropertiesRepository(MediaOpsPlanApi planApi)
			: base(planApi)
		{
		}

		public long Count()
		{
			return Count(new TRUEFilterElement<Property>());
		}

		public long Count(FilterElement<Property> filter)
		{
			if (filter.isEmpty())
			{
				return 0;
			}

			return PlanApi.DomHelpers.SlcPropertiesHelper.CountPropertiesInstances(filterTranslator.Translate(filter));
		}

		public long Count(IQuery<Property> query)
		{
			return Count(query.Filter);
		}

		public IReadOnlyCollection<Property> Create(IEnumerable<Property> oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			var list = oToCreate.ToList();

			var existing = list.Where(x => !x.IsNew);
			if (existing.Any())
			{
				throw new InvalidOperationException("Not possible to use method Create for existing property definitions. Use CreateOrUpdate or Update instead.");
			}

			if (!SchedulingPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return Property.InstantiateProperties(result.SuccessfulItems).ToList();
		}

		public Property Create(Property oToCreate)
		{
			if (oToCreate == null)
			{
				throw new ArgumentNullException(nameof(oToCreate));
			}

			if (!oToCreate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Create for existing property definition. Use CreateOrUpdate or Update instead.");
			}

			if (!SchedulingPropertyHandler.TryCreateOrUpdate(PlanApi, [oToCreate], out var result))
			{
				result.ThrowSingleException(oToCreate.Id);
			}

			return Property.InstantiateProperty(result.SuccessfulItems.Single());
		}

		public IReadOnlyCollection<Property> CreateOrUpdate(IEnumerable<Property> oToCreateOrUpdate)
		{
			if (oToCreateOrUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToCreateOrUpdate));
			}

			var list = oToCreateOrUpdate.ToList();

			if (!SchedulingPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return Property.InstantiateProperties(result.SuccessfulItems).ToList();
		}

		public void Delete(IEnumerable<Property> properties, PropertyDeleteOptions options)
		{
			if (properties == null)
			{
				throw new ArgumentNullException(nameof(properties));
			}

			Delete(properties.Select(x => x.Id).ToArray(), options);
		}

		public void Delete(IEnumerable<Guid> propertyIds, PropertyDeleteOptions options)
		{
			if (propertyIds == null)
			{
				throw new ArgumentNullException(nameof(propertyIds));
			}

			var toDelete = Read(propertyIds.ToArray());

			if (!SchedulingPropertyHandler.TryDelete(PlanApi, toDelete.ToList(), out var result, options))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(Property property, PropertyDeleteOptions options)
		{
			if (property == null)
			{
				throw new ArgumentNullException(nameof(property));
			}

			Delete(property.Id, options);
		}

		public void Delete(Guid propertyId, PropertyDeleteOptions options)
		{
			var toDelete = Read(propertyId);
			if (toDelete == null)
			{
				return;
			}

			if (!SchedulingPropertyHandler.TryDelete(PlanApi, [toDelete], out var result, options))
			{
				result.ThrowSingleException(toDelete.Id);
			}
		}

		public void Delete(Guid apiObjectId)
		{
			var toDelete = Read(apiObjectId);
			if (toDelete == null)
			{
				return;
			}

			if (!SchedulingPropertyHandler.TryDelete(PlanApi, [toDelete], out var result))
			{
				result.ThrowSingleException(toDelete.Id);
			}
		}

		public void Delete(IEnumerable<Guid> apiObjectIds)
		{
			if (apiObjectIds == null)
			{
				throw new ArgumentNullException(nameof(apiObjectIds));
			}

			var toDelete = Read(apiObjectIds.ToArray());

			if (!SchedulingPropertyHandler.TryDelete(PlanApi, toDelete.ToList(), out var result))
			{
				result.ThrowBulkException();
			}
		}

		public void Delete(IEnumerable<Property> oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Select(x => x.Id).ToArray());
		}

		public void Delete(Property oToDelete)
		{
			if (oToDelete == null)
			{
				throw new ArgumentNullException(nameof(oToDelete));
			}

			Delete(oToDelete.Id);
		}

		public IEnumerable<Property> Read()
		{
			return Read(new TRUEFilterElement<Property>());
		}

		public Property Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException(nameof(id));
			}

			return Read(PropertyExposers.Id.Equal(id)).FirstOrDefault();
		}

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

		public IEnumerable<Property> Read(FilterElement<Property> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			if (filter.isEmpty())
			{
				return Enumerable.Empty<Property>();
			}

			var properties = PlanApi.DomHelpers.SlcPropertiesHelper.GetProperties(filterTranslator.Translate(filter));
			return Property.InstantiateProperties(properties);
		}

		public IEnumerable<Property> Read(IQuery<Property> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return Read(query.Filter);
		}

		public IEnumerable<IPagedResult<Property>> ReadPaged()
		{
			return ReadPaged(new TRUEFilterElement<Property>());
		}

		public IEnumerable<IPagedResult<Property>> ReadPaged(int pageSize)
		{
			return ReadPaged(new TRUEFilterElement<Property>(), pageSize);
		}

		public IEnumerable<IPagedResult<Property>> ReadPaged(FilterElement<Property> filter)
		{
			return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
		}

		public IEnumerable<IPagedResult<Property>> ReadPaged(IQuery<Property> query)
		{
			return ReadPaged(query.Filter);
		}

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

		public IEnumerable<IPagedResult<Property>> ReadPaged(IQuery<Property> query, int pageSize)
		{
			return ReadPaged(query.Filter, pageSize);
		}

		public IReadOnlyCollection<Property> Update(IEnumerable<Property> oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			var list = oToUpdate.ToList();

			var newItems = list.Where(x => x.IsNew);
			if (newItems.Any())
			{
				throw new InvalidOperationException("Not possible to use method Update for new property definitions. Use Create or CreateOrUpdate instead.");
			}

			if (!SchedulingPropertyHandler.TryCreateOrUpdate(PlanApi, list, out var result))
			{
				result.ThrowBulkException();
			}

			return Property.InstantiateProperties(result.SuccessfulItems).ToList();
		}

		public Property Update(Property oToUpdate)
		{
			if (oToUpdate == null)
			{
				throw new ArgumentNullException(nameof(oToUpdate));
			}

			if (oToUpdate.IsNew)
			{
				throw new InvalidOperationException("Not possible to use method Update for new property definition. Use Create or CreateOrUpdate instead.");
			}

			if (!SchedulingPropertyHandler.TryCreateOrUpdate(PlanApi, [oToUpdate], out var result))
			{
				result.ThrowSingleException(oToUpdate.Id);
			}

			return Property.InstantiateProperty(result.SuccessfulItems.Single());
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
