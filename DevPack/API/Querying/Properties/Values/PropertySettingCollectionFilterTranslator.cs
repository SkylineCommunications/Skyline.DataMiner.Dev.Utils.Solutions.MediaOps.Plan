namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal class PropertySettingCollectionFilterTranslator : DomInstanceFilterTranslator<PropertySettingCollection>
	{
		private readonly FilterElement<DomInstance> propertySettingsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.PropertyValues.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[PropertySettingCollectionExposers.Id.fieldName] = HandleGuid,
			[PropertySettingCollectionExposers.LinkedObjectId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.LinkedObjectID), comparer, (string)value),
			[PropertySettingCollectionExposers.Scope.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.Scope), comparer, (string)value),
			[PropertySettingCollectionExposers.SubId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.SubID), comparer, (string)value),
			[PropertySettingCollectionExposers.PropertySettings.PropertyId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValue.PropertyID), comparer, (Guid)value),
		};

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		protected override FilterElement<DomInstance> DomDefinitionFilter => propertySettingsDomDefinitionFilter;
	}
}
