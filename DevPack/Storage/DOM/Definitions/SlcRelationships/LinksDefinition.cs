namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcRelationships
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;

	internal sealed class LinksDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Links")
		{
			ID = SlcRelationshipsIds.Definitions.Links,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcRelationshipsIds.Sections.LinkInfo.Id) { IsOptional = false, AllowMultipleSections = false },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides(),
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetLinkInfo() };

		private static CustomSectionDefinition GetLinkInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcRelationshipsIds.Sections.LinkInfo.Id, Name = "Link Info" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcRelationshipsIds.ModuleId)
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ChildObjectType,
				Name = "Child object type",
				Tooltip = String.Empty,
				FieldType = typeof(Guid),
				IsOptional = false,
				DomDefinitionIds = { SlcRelationshipsIds.Definitions.ObjectTypes },
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ChildObjectID,
				Name = "Child object ID",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ChildObjectName,
				Name = "Child object name",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ChildURL,
				Name = "Child URL",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ChildOrder,
				Name = "Child Order",
				FieldType = typeof(long),
				IsOptional = true,
				Tooltip = "Indicates the visual position of the relationship at the Child",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(SlcRelationshipsIds.ModuleId)
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ParentObjectType,
				Name = "Parent object type",
				Tooltip = String.Empty,
				FieldType = typeof(Guid),
				IsOptional = false,
				DomDefinitionIds = { SlcRelationshipsIds.Definitions.ObjectTypes },
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ParentObjectID,
				Name = "Parent object ID",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ParentObjectName,
				Name = "Parent object name",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ParentURL,
				Name = "Parent URL",
				Tooltip = String.Empty,
				FieldType = typeof(string),
				IsOptional = true,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.LinkInfo.ParentOrder,
				Name = "Parent Order",
				Tooltip = String.Empty,
				FieldType = typeof(long),
				IsOptional = true,
			});
			return section;
		}
	}
}
