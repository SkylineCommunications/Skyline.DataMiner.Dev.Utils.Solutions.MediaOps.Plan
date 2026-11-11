namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class AppSettingsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("App settings")
		{
			ID = Ids.Definitions.AppSettings,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks = { new SectionDefinitionLink(Ids.Sections.JobSettings.Id) { IsOptional = false, AllowMultipleSections = false } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetJobSettings() };

		private static CustomSectionDefinition GetJobSettings()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobSettings.Id, Name = "Job settings" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.JobIDPrefix, Name = "Job ID prefix", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The prefix is a string that will be prepended to each Job ID.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.JobIDMinimumDigits, Name = "Job ID minimum digits", FieldType = typeof(long), IsOptional = true,
				Tooltip = "Defines the minimum number of digits in the auto-generated value, which must be between 1 and 20.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.JobIDStartingSeed, Name = "Job ID starting seed", FieldType = typeof(long), IsOptional = true,
				Tooltip = "Provides the \"starting seed\" when first configuring the job IDs.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.JobIDIncrement, Name = "Job ID increment", FieldType = typeof(long), IsOptional = true,
				Tooltip = "An integer defining the increment by which the auto-generated ID increases.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.JobIDNextSequence, Name = "Job ID next sequence", FieldType = typeof(long), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.DefaultPreroll, Name = "Default Preroll", FieldType = typeof(TimeSpan), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobSettings.DefaultPostroll, Name = "Default Postroll", FieldType = typeof(TimeSpan), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobSettings.DesiredJobStatus, Name = "Desired Job Status", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("Desired Job Status", ("Draft", 0), ("Tentative", 1)), Tooltip = "The desired job status on job creation",
			});
			return section;
		}
	}
}
