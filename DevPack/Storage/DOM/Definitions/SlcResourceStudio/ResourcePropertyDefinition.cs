namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal sealed class ResourcePropertyDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("ResourceProperty")
		{
			ID = SlcResource_StudioIds.Definitions.Resourceproperty,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks = { new SectionDefinitionLink(SlcResource_StudioIds.Sections.PropertyInfo.Id) { IsOptional = false, AllowMultipleSections = false } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetPropertyInfo() };

		private static CustomSectionDefinition GetPropertyInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.PropertyInfo.Id, Name = "Property info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.PropertyInfo.PropertyName,
				Name = "Property name",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			return section;
		}
	}
}
