namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	internal sealed class FlowRulesDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("FlowRules")
		{
			ID = SlcResource_StudioIds.Definitions.Flowrules,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.Flowrulesinfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(SlcResource_StudioIds.Sections.Flowrule.Id) { IsOptional = true, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetFlowRulesInfo(), GetFlowRule() };

		private static CustomSectionDefinition GetFlowRulesInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.Flowrulesinfo.Id, Name = "FlowRulesInfo" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrulesinfo.Owner,
				Name = "Owner",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrulesinfo.Ownertype,
				Name = "OwnerType",
				FieldType = typeof(GenericEnum<int>),
				IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("FlowRuleObjectType", ("ResourcePool", (int)SlcResource_StudioIds.Enums.Flowruleobjecttype.Resourcepool)),
				Tooltip = String.Empty,
			});
			return section;
		}

		private static CustomSectionDefinition GetFlowRule()
		{
			var section = new CustomSectionDefinition { ID = SlcResource_StudioIds.Sections.Flowrule.Id, Name = "FlowRule" };
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrule.Direction,
				Name = "Direction",
				FieldType = typeof(GenericEnum<int>),
				IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("FlowRuleDirection", ("Before", (int)SlcResource_StudioIds.Enums.Flowruledirection.Before), ("After", (int)SlcResource_StudioIds.Enums.Flowruledirection.After)),
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrule.Target,
				Name = "Target",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrule.Targettype,
				Name = "TargetType",
				FieldType = typeof(GenericEnum<int>),
				IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("FlowRuleObjectType", ("ResourcePool", (int)SlcResource_StudioIds.Enums.Flowruleobjecttype.Resourcepool)),
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrule.Outcome,
				Name = "Outcome",
				FieldType = typeof(GenericEnum<int>),
				IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("FlowRuleOutcome", ("Allowed", (int)SlcResource_StudioIds.Enums.Flowruleoutcome.Allowed), ("Warning", (int)SlcResource_StudioIds.Enums.Flowruleoutcome.Warning), ("Forbidden", (int)SlcResource_StudioIds.Enums.Flowruleoutcome.Forbidden)),
				Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcResource_StudioIds.Sections.Flowrule.Message,
				Name = "Message",
				FieldType = typeof(string),
				IsOptional = true,
				Tooltip = String.Empty,
			});
			return section;
		}
	}
}
