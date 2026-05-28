namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

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

		protected override FilterElement<DomInstance> DomDefinitionFilter => propertyDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;
	}
}
