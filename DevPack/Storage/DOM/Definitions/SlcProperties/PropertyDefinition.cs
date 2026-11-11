namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal sealed class PropertyDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Property")
		{
			ID = SlcPropertiesIds.Definitions.Property,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcPropertiesIds.Sections.PropertyInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcPropertiesIds.Sections.Discrete.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcPropertiesIds.Sections.Layout.Id) { IsOptional = true, AllowMultipleSections = false },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems = { new FieldValueConcatenationItem(SlcPropertiesIds.Sections.PropertyInfo.Name) },
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetPropertyInfo(),
			GetDiscrete(),
			GetLayout(),
		};

		private static CustomSectionDefinition GetPropertyInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcPropertiesIds.Sections.PropertyInfo.Id, Name = "Property Info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.Name,
				Name = "Name",
				FieldType = typeof(string),
				IsOptional = false,
				Tooltip = "Name of the property",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.PropertyType,
				Name = "Property Type",
				FieldType = typeof(GenericEnum<string>),
				IsOptional = false,
				Tooltip = "Type of the property",
				GenericEnumInstance = GetPropertyTypeEnum(),
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.Scope,
				Name = "Scope",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = "Property Scope",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.Default,
				Name = "Default",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.StringSizeLimit,
				Name = "String Size Limit",
				Tooltip = String.Empty,
				FieldType = typeof(long),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.IsMultiLineString,
				Name = "Is Multi Line String",
				Tooltip = String.Empty,
				FieldType = typeof(bool),
				IsOptional = true,
				DefaultValue = new ValueWrapper<bool>(false),
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.FileSizeLimit,
				Name = "File Size Limit",
				Tooltip = String.Empty,
				FieldType = typeof(long),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.PropertyInfo.AllowMultipleFiles,
				Name = "Allow Multiple Files",
				Tooltip = String.Empty,
				FieldType = typeof(bool),
				IsOptional = true,
				DefaultValue = new ValueWrapper<bool>(false),
			});
			return section;
		}

		private static GenericEnum<string> GetPropertyTypeEnum()
		{
			var values = new GenericEnum<string>();
			values.AddEntry("String", SlcPropertiesIds.Enums.Propertytype.String);
			values.AddEntry("Discrete", SlcPropertiesIds.Enums.Propertytype.Discrete);
			values.AddEntry("Boolean", SlcPropertiesIds.Enums.Propertytype.Boolean);
			values.AddEntry("File", SlcPropertiesIds.Enums.Propertytype.File);
			return values;
		}

		private static CustomSectionDefinition GetDiscrete()
		{
			var section = new CustomSectionDefinition { ID = SlcPropertiesIds.Sections.Discrete.Id, Name = "Discrete" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.Discrete.Option,
				Name = "Option",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = false,
			});
			return section;
		}

		private static CustomSectionDefinition GetLayout()
		{
			var section = new CustomSectionDefinition { ID = SlcPropertiesIds.Sections.Layout.Id, Name = "Layout" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.Layout.SectionName,
				Name = "Section Name",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPropertiesIds.Sections.Layout.Order,
				Name = "Order",
				Tooltip = String.Empty,
				FieldType = typeof(long),
				IsOptional = true,
			});
			return section;
		}
	}
}
