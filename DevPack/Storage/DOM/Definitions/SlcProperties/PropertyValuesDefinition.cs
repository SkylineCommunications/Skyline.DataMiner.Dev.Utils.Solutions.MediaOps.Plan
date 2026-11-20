namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal sealed class PropertyValuesDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Property Values")
		{
			ID = SlcPropertiesIds.Definitions.PropertyValues,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcPropertiesIds.Sections.PropertyValueInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcPropertiesIds.Sections.PropertyValue.Id) { IsOptional = true, AllowMultipleSections = true },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetPropertyValueInfo(), GetPropertyValue() };

		private static CustomSectionDefinition GetPropertyValueInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcPropertiesIds.Sections.PropertyValueInfo.Id, Name = "Property Value Info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyValueInfo.LinkedObjectID,
				Name = "Linked Object ID",
				FieldType = typeof(string),
				IsOptional = false,
				Tooltip = "Linked Object",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyValueInfo.Scope,
				Name = "Scope",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = "The scope in which this property value was created",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyValueInfo.SubID,
				Name = "Sub ID",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = "Sub identifier to which these property values are linked.",
			});
			return section;
		}

		private static CustomSectionDefinition GetPropertyValue()
		{
			var section = new CustomSectionDefinition { ID = SlcPropertiesIds.Sections.PropertyValue.Id, Name = "Property Value" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyValue.PropertyName,
				Name = "Property Name",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = "Property Name",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyValue.Value,
				Name = "Value",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = "Property Value",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcPropertiesIds.ModuleId)
			{
				ID = SlcPropertiesIds.Sections.PropertyValue.PropertyID,
				Name = "Property ID",
				Tooltip = String.Empty,
				FieldType = typeof(Guid),
				IsOptional = true,
				DomDefinitionIds = { SlcPropertiesIds.Definitions.Property },
			});
			return section;
		}
	}
}
