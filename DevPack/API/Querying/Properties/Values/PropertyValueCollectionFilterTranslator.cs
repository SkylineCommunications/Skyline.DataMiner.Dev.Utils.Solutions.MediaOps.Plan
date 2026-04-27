namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal class PropertyValueCollectionFilterTranslator : DomInstanceFilterTranslator<PropertyValueCollection>
	{
		private readonly FilterElement<DomInstance> propertyValuesDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.PropertyValues.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[PropertyValueCollectionExposers.Id.fieldName] = HandleGuid,
			[PropertyValueCollectionExposers.LinkedObjectId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.LinkedObjectID), comparer, (string)value),
			[PropertyValueCollectionExposers.Scope.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.Scope), comparer, (string)value),
			[PropertyValueCollectionExposers.SubId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.SubID), comparer, (string)value),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => propertyValuesDomDefinitionFilter;
	}
}
