namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class WorkflowsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Workflows")
		{
			ID = Ids.Definitions.Workflows,
			DomBehaviorDefinitionId = Ids.Behaviors.Workflow_Behavior.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.WorkflowInfo.WorkflowName) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.WorkflowInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.WorkflowExecution.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.MonitoringSettings.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.Nodes.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Connections.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.NodeRelationships.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.NodeGroups.Id) { IsOptional = false, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			GetWorkflowInfo(), GetWorkflowExecution(), WorkflowSectionDefinitions.GetMonitoringSettings(), WorkflowSectionDefinitions.GetNodes(),
			WorkflowSectionDefinitions.GetConnections(), WorkflowSectionDefinitions.GetNodeRelationships(), WorkflowSectionDefinitions.GetNodeGroups(),
		};

		private static CustomSectionDefinition GetWorkflowInfo()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.WorkflowInfo.Id, Name = "Workflow info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.WorkflowName, Name = "Workflow name", FieldType = typeof(string), IsOptional = false, Tooltip = "A short name for the workflow. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.WorkflowDescription, Name = "Workflow description", FieldType = typeof(string), IsOptional = true,
				Tooltip = "A description of the workflow, which can include its purpose, steps, and functionality.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.Favorite, Name = "Favorite", FieldType = typeof(bool), IsOptional = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = "Flag that indicates whether this workflow is a favorite. ",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.Priority, Name = "Priority", FieldType = typeof(GenericEnum<int>), IsOptional = false,
				DefaultValue = new ValueWrapper<int>((int)Ids.Enums.Priority.Normal),
				GenericEnumInstance = GenericEnumFactory.Create("priority", ("High", 1), ("Normal", 2), ("Low", 3)), Tooltip = "Describes the relative importance of the workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.WorkflowNotes, Name = "Workflow notes", FieldType = typeof(string), IsOptional = true,
				Tooltip = "A general note field for workflow-related communication.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.LockedBy, Name = "Locked by", FieldType = typeof(string), IsOptional = true, IsHidden = true, IsSoftDeleted = true,
				Tooltip = "Indicates who locked this workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.Preroll, Name = "Preroll", FieldType = typeof(TimeSpan), IsOptional = true,
				Tooltip = "An extra time configured before a job's start time to reserve resources",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.Postroll, Name = "Postroll", FieldType = typeof(TimeSpan), IsOptional = true,
				Tooltip = "An extra time configured after a job's end time to take down the workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowInfo.JobType, Name = "Job type", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The ID of the job type category that has to be used for jobs created from this workflow.",
			});
			return section;
		}

		private static CustomSectionDefinition GetWorkflowExecution()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.WorkflowExecution.Id, Name = "Workflow execution" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.WorkflowExecution.WorkflowExecutionScript, Name = "Workflow execution script", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Specifies a script to be triggered when this workflow is executed (optional).",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.WorkflowExecution.WorkflowConfiguration, Name = "Workflow configuration", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Configuration }, Tooltip = String.Empty,
			});
			return section;
		}
	}
}
