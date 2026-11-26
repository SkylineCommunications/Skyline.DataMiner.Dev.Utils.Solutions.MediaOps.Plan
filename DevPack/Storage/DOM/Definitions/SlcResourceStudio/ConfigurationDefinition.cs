namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal sealed class ConfigurationDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Configuration")
		{
			ID = SlcResource_StudioIds.Definitions.Configuration,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.ProfileParameterValues.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.OrchestrationEvents.Id) { IsOptional = true, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetProfileParameterValues(), GetOrchestrationEvents() };

		private static CustomSectionDefinition GetProfileParameterValues()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.ProfileParameterValues.Id, Name = "Profile parameter values" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ProfileParameterValues.ProfileParameterID,
				Name = "Profile parameter ID",
				FieldType = typeof(string),
				IsOptional = false,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ProfileParameterValues.StringValue,
				Name = "String value",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ProfileParameterValues.DoubleMaxValue,
				Name = "Double max value",
				FieldType = typeof(double),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ProfileParameterValues.DoubleMinValue,
				Name = "Double min value",
				FieldType = typeof(double),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.ProfileParameterValues.Reference,
				Name = "Reference",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetOrchestrationEvents()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.OrchestrationEvents.Id, Name = "Orchestration events" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.OrchestrationEvents.Name,
				Name = "Name",
				FieldType = typeof(string),
				IsOptional = false,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.OrchestrationEvents.Script,
				Name = "Script",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.OrchestrationEvents.ScriptInput,
				Name = "Script input",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			return section;
		}
	}
}
