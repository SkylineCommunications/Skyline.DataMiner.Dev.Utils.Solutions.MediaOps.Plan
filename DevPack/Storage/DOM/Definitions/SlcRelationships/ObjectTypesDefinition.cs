namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcRelationships
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;

	internal sealed class ObjectTypesDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Object Types")
		{
			ID = SlcRelationshipsIds.Definitions.ObjectTypes,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(SlcRelationshipsIds.Sections.ObjectTypeInfo.Id) { IsOptional = false, AllowMultipleSections = false },
			},
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition
				{
					ConcatenationItems = { new FieldValueConcatenationItem(SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName) },
				},
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetObjectTypeInfo() };

		private static CustomSectionDefinition GetObjectTypeInfo()
		{
			var section = new CustomSectionDefinition { ID = SlcRelationshipsIds.Sections.ObjectTypeInfo.Id, Name = "Object Type Info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName,
				Name = "Object name",
				FieldType = typeof(string),
				IsOptional = false,
				Tooltip = "e.g. Event, Task, SchALL WO, Document, PO",
			});
			return section;
		}
	}
}
