namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class JobNodeRelationshipActionsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Job node relationship actions")
		{
			ID = Ids.Definitions.JobNodeRelationshipActions,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.JobNodeRelationshipGeneralActions.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.JobNodeRelationshipReplaceActions.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetGeneralActions(), GetReplaceActions() };

		private static CustomSectionDefinition GetGeneralActions()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobNodeRelationshipGeneralActions.Id, Name = "Job node relationship general actions" };
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobNodeRelationshipGeneralActions.BookingAction, Name = "Booking action", FieldType = typeof(GenericEnum<int>), IsOptional = false, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("Booking Action", ("All or Nothing", 0), ("Best Effort", 1)), Tooltip = "Action taken when a node is added to a job.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobNodeRelationshipGeneralActions.DeleteAction, Name = "Delete action", FieldType = typeof(GenericEnum<int>), IsOptional = false, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("Delete Action", ("Single Node", 0), ("All Nodes", 1)), Tooltip = "Action taken when a node is removed from a job.",
			});
			return section;
		}

		private static CustomSectionDefinition GetReplaceActions()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobNodeRelationshipReplaceActions.Id, Name = "Job node relationship replace actions" };
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobNodeRelationshipReplaceActions.ReplaceAction, Name = "Replace action", FieldType = typeof(GenericEnum<int>), IsOptional = false, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("Replace Action", ("Single Node", 0), ("All Nodes", 1)), Tooltip = "Action taken when a node is replaced in a job.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobNodeRelationshipReplaceActions.ExecutePoolLinks, Name = "Execute pool links", FieldType = typeof(bool), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = "Defines whether to apply pool links (if available) during a replace action.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobNodeRelationshipReplaceActions.ExecuteBookingExtensionScript, Name = "Execute booking extension script", FieldType = typeof(bool), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = "Defines whether to execute booking extension scripts (if available) during a replace action.",
			});
			return section;
		}
	}
}
