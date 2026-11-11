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

	internal sealed class RecurringJobsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Recurring jobs")
		{
			ID = Ids.Definitions.RecurringJobs,
			DomBehaviorDefinitionId = Ids.Behaviors.Recurringjob_Behavior.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.JobInfo.JobName) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.JobInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.Nodes.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Connections.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.RecurringInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.NodeRelationships.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Errors.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.MonitoringSettings.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.JobExecution.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.CostingAndBilling.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.NodeGroups.Id) { IsOptional = false, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			WorkflowSectionDefinitions.GetJobInfo(), WorkflowSectionDefinitions.GetNodes(), WorkflowSectionDefinitions.GetConnections(), GetRecurringInfo(),
			WorkflowSectionDefinitions.GetNodeRelationships(), WorkflowSectionDefinitions.GetErrors(), WorkflowSectionDefinitions.GetMonitoringSettings(),
			WorkflowSectionDefinitions.GetJobExecution(), WorkflowSectionDefinitions.GetCostingAndBilling(), WorkflowSectionDefinitions.GetNodeGroups(),
		};

		private static CustomSectionDefinition GetRecurringInfo()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.RecurringInfo.Id, Name = "Recurring Info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.RecurringInfo.Duration, Name = "Duration", FieldType = typeof(TimeSpan), IsOptional = false, Tooltip = "Duration of the job",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.RecurringInfo.TimeZone, Name = "Time zone", FieldType = typeof(string), IsOptional = false,
				Tooltip = "The time zone in which the jobs will be created",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.RecurringInfo.RecurringPattern, Name = "Recurring pattern", FieldType = typeof(string), IsOptional = false, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.RecurringInfo.ProcessStatus, Name = "Process Status", FieldType = typeof(GenericEnum<int>), IsOptional = false,
				GenericEnumInstance = GenericEnumFactory.Create("Process Status", ("NA", 0), ("Updating Series", 1), ("Series Updated", 2), ("Update failed", 3), ("Canceling Series", 4)),
				Tooltip = "The status of the job creation that are linked to this series",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.RecurringInfo.DesiredJobStatus, Name = "Desired Job Status", FieldType = typeof(GenericEnum<int>), IsOptional = false,
				GenericEnumInstance = GenericEnumFactory.Create("Desired Job Status", ("Draft", 0), ("Tentative", 1)),
				Tooltip = "The desired status of the jobs that are going to be created based on recurring job",
			});
			return section;
		}
	}
}
