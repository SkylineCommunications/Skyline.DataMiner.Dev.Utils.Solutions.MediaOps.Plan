namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	using SLDataGateway.API.Types.Querying;

	internal class SchedulingPropertyFilterTranslator : DomInstanceFilterTranslator<Property>
	{
		private readonly FilterElement<DomInstance> propertyDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.Property.Id).AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyInfo.Scope).Equal(SchedulingPropertyHandler.Scope));
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[PropertyExposers.Id.fieldName] = HandleGuid,
			[PropertyExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyInfo.Name), comparer, (string)value),
			[PropertyExposers.SectionName.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.Layout.SectionName), comparer, (string)value),
			[PropertyExposers.Order.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.Layout.Order), comparer, (int)value),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[PropertyExposers.Id.fieldName] = HandleGuid,
			[PropertyExposers.Name.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyInfo.Name), sortOrder, naturalSort),
			[PropertyExposers.SectionName.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.Layout.SectionName), sortOrder, naturalSort),
			[PropertyExposers.Order.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.Layout.Order), sortOrder, naturalSort),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => propertyDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> FilterHandlers => handlers;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;
	}
}
