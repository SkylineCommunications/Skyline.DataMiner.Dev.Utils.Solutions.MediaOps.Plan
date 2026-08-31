namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Types.Querying;

	internal class SlcRelationshipsHelper : DomModuleHelperBase
	{
		public SlcRelationshipsHelper(IConnection connection) : base(SlcRelationshipsIds.ModuleId, connection)
		{
		}

		public long CountRelationshipsInstances(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return DomHelper.DomInstances.Count(filter);
		}

		public long CountRelationshipsInstances(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return DomHelper.DomInstances.Count(query);
		}

		public IEnumerable<ObjectTypesInstance> GetObjectTypes(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<ObjectTypesInstance>();
			}

			FilterElement<DomInstance> filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcRelationshipsIds.Definitions.ObjectTypes.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => filter(x),
				x => GetObjectTypeIterator(x));
		}

		public IEnumerable<ObjectTypesInstance> GetObjectTypes(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetObjectTypeIterator(filter);
		}

		public IEnumerable<ObjectTypesInstance> GetObjectTypes(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return GetObjectTypeIterator(query);
		}

		public IEnumerable<ObjectTypesInstance> GetObjectTypes<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
		{
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				values.Distinct(),
				x => filter(x),
				x => GetObjectTypeIterator(x));
		}

		public IEnumerable<LinksInstance> GetLinks(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<LinksInstance>();
			}

			FilterElement<DomInstance> filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcRelationshipsIds.Definitions.Links.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => filter(x),
				x => GetLinkIterator(x));
		}

		public IEnumerable<LinksInstance> GetLinks(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetLinkIterator(filter);
		}

		public IEnumerable<LinksInstance> GetLinks(IQuery<DomInstance> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			return GetLinkIterator(query);
		}

		public IEnumerable<LinksInstance> GetLinks<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
		{
			if (values == null)
			{
				throw new ArgumentNullException(nameof(values));
			}

			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				values.Distinct(),
				x => filter(x),
				x => GetLinkIterator(x));
		}

		// DOM instance IDs are unique per module, not per definition, so ID collisions must be looked up across both definitions.
		public IEnumerable<DomInstance> GetRelationshipsInstances(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<DomInstance>();
			}

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => DomInstanceExposers.Id.Equal(x),
				x => DomHelper.DomInstances.Read(x));
		}

		internal IEnumerable<IEnumerable<ObjectTypesInstance>> GetObjectTypesPaged(FilterElement<DomInstance> paramFilter, int pageSize)
		{
			if (paramFilter == null)
			{
				throw new ArgumentNullException(nameof(paramFilter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(paramFilter, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new ObjectTypesInstance(instance));
		}

		internal IEnumerable<IEnumerable<ObjectTypesInstance>> GetObjectTypesPaged(IQuery<DomInstance> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			return InstanceFactory.ReadAndCreateInstancesPaged(DomHelper, query, pageSize, instance => new ObjectTypesInstance(instance));
		}

		internal IEnumerable<IEnumerable<LinksInstance>> GetLinksPaged(FilterElement<DomInstance> paramFilter, int pageSize)
		{
			if (paramFilter == null)
			{
				throw new ArgumentNullException(nameof(paramFilter));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			var pages = DomHelper.DomInstances.ReadPaged(paramFilter, pageSize);
			return InstanceFactory.CreateInstances(pages, instance => new LinksInstance(instance));
		}

		internal IEnumerable<IEnumerable<LinksInstance>> GetLinksPaged(IQuery<DomInstance> query, int pageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			if (pageSize <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(pageSize));
			}

			return InstanceFactory.ReadAndCreateInstancesPaged(DomHelper, query, pageSize, instance => new LinksInstance(instance));
		}

		private IEnumerable<ObjectTypesInstance> GetObjectTypeIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ObjectTypesInstance(instance));
		}

		private IEnumerable<ObjectTypesInstance> GetObjectTypeIterator(IQuery<DomInstance> query)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new ObjectTypesInstance(instance));
		}

		private IEnumerable<LinksInstance> GetLinkIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new LinksInstance(instance));
		}

		private IEnumerable<LinksInstance> GetLinkIterator(IQuery<DomInstance> query)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new LinksInstance(instance));
		}
	}
}
