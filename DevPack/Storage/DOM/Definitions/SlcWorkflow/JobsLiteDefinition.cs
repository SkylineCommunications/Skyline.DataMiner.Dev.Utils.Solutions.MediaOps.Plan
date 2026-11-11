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

	internal sealed class JobsLiteDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Jobs lite")
		{
			ID = Ids.Definitions.JobsLite,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.JobInfo.JobName) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.JobInfo.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.CostingAndBilling.Id) { IsOptional = true, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.Errors.Id) { IsOptional = true, AllowMultipleSections = true, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.JobLiteMetadata.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			WorkflowSectionDefinitions.GetJobInfo(), WorkflowSectionDefinitions.GetCostingAndBilling(), WorkflowSectionDefinitions.GetErrors(), GetJobLiteMetadata(),
		};

		private static CustomSectionDefinition GetJobLiteMetadata()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobLiteMetadata.Id, Name = "Job lite metadata" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.JobLiteMetadata.FullJobID, Name = "Full job ID", FieldType = typeof(Guid), IsOptional = false, IsSoftDeleted = true,
				DomDefinitionIds = { Ids.Definitions.Jobs }, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobLiteMetadata.JobState, Name = "Job state", FieldType = typeof(GenericEnum<int>), IsOptional = false, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<int>((int)Ids.Enums.Jobstate.Draft),
				GenericEnumInstance = GenericEnumFactory.Create("Job State", ("Draft", 0), ("Tentative", 1), ("Confirmed", 2), ("Running", 3), ("Completed", 4), ("Ready For invoice", 5), ("Canceled", 6), ("Invoiced", 7)),
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobLiteMetadata.LinkedResourcesMetadata, Name = "Linked resources metadata", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobLiteMetadata.JobDuration, Name = "Job duration", FieldType = typeof(TimeSpan), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			return section;
		}
	}
}
