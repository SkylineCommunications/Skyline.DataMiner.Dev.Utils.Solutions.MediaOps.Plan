namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Settings;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class ConfigurationDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Configuration")
		{
			ID = Ids.Definitions.Configuration,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				ScriptSettings = new ExecuteScriptOnDomInstanceActionSettings
				{
					OnCreate = "WorkflowDesigner_CRUD_Workflow", OnDelete = "WorkflowDesigner_CRUD_Workflow", OnUpdate = "WorkflowDesigner_CRUD_Workflow",
					ScriptType = OnDomInstanceActionScriptType.FullCrudMeta,
				},
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.ProfileParameterValues.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.OrchestrationEvents.Id) { IsOptional = true, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetProfileParameterValues(), GetOrchestrationEvents() };

		private static CustomSectionDefinition GetProfileParameterValues()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.ProfileParameterValues.Id, Name = "Profile parameter values" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ProfileParameterValues.ProfileParameterID, Name = "Profile parameter ID", FieldType = typeof(string), IsOptional = false, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ProfileParameterValues.StringValue, Name = "String value", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ProfileParameterValues.DoubleMaxValue, Name = "Double max value", FieldType = typeof(double), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ProfileParameterValues.DoubleMinValue, Name = "Double min value", FieldType = typeof(double), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ProfileParameterValues.Reference, Name = "Reference", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetOrchestrationEvents()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.OrchestrationEvents.Id, Name = "Orchestration Events" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrchestrationEvents.Name, Name = "Name", FieldType = typeof(string), IsOptional = false, Tooltip = "Name of the orchestration event",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrchestrationEvents.Script, Name = "Script", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Name of the automation script to be executed at the orchestration event time",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrchestrationEvents.ScriptInputValues, Name = "Script input values", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Serialized instance containing the script input values, etc.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrchestrationEvents.Metadata, Name = "Metadata", FieldType = typeof(string), IsOptional = true, Tooltip = "Optional metadata field",
			});
			return section;
		}
	}
}
