namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	internal class SlcPropertiesHelper : DomModuleHelperBase
	{
		public SlcPropertiesHelper(IConnection connection) : base(SlcPropertiesIds.ModuleId, connection)
		{
		}

		public long CountPropertiesInstances(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return DomHelper.DomInstances.Count(filter);
		}

		public IEnumerable<DomInstance> GetPropertiesInstances(IEnumerable<Guid> ids)
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

		public IEnumerable<PropertyInstance> GetProperties(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<PropertyInstance>();
			}

			FilterElement<DomInstance> filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.Property.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => filter(x),
				x => GetPropertyIterator(x));
		}

		public IEnumerable<PropertyInstance> GetProperties(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetPropertyIterator(filter);
		}

		public IEnumerable<PropertyInstance> GetProperties<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
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
				x => GetPropertyIterator(x));
		}

		public IEnumerable<PropertyValuesInstance> GetPropertyValues(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			if (!ids.Any())
			{
				return Enumerable.Empty<PropertyValuesInstance>();
			}

			FilterElement<DomInstance> filter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.PropertyValues.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
				ids.Distinct(),
				x => filter(x),
				x => GetPropertyValueIterator(x));
		}

		public IEnumerable<PropertyValuesInstance> GetPropertyValues(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetPropertyValueIterator(filter);
		}

		internal IEnumerable<IEnumerable<PropertyInstance>> GetPropertiesPaged(FilterElement<DomInstance> paramFilter, int pageSize)
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
			return InstanceFactory.CreateInstances(pages, instance => new PropertyInstance(instance));
		}

		internal IEnumerable<IEnumerable<PropertyValuesInstance>> GetPropertyValuesPaged(FilterElement<DomInstance> paramFilter, int pageSize)
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
			return InstanceFactory.CreateInstances(pages, instance => new PropertyValuesInstance(instance));
		}

		private IEnumerable<PropertyInstance> GetPropertyIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new PropertyInstance(instance));
		}

		private IEnumerable<PropertyValuesInstance> GetPropertyValueIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new PropertyValuesInstance(instance));
		}
	}
}
