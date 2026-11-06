namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	internal sealed class CategoryDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Category")
		{
			ID = SlcPeopleOrganizationsIds.Definitions.Category,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(SlcPeopleOrganizationsIds.Sections.CategoryInformation.Category) } },
			},
			SectionDefinitionLinks = { new SectionDefinitionLink(SlcPeopleOrganizationsIds.Sections.CategoryInformation.Id) { IsOptional = false, AllowMultipleSections = false } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetCategoryInformation() };

		private static CustomSectionDefinition GetCategoryInformation()
		{
			var section = new CustomSectionDefinition { ID = SlcPeopleOrganizationsIds.Sections.CategoryInformation.Id, Name = "Category information" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPeopleOrganizationsIds.Sections.CategoryInformation.Category, Name = "Category", FieldType = typeof(string), IsOptional = false,
				Tooltip = "For example, Software Company, Social Media, Production Studio, etc.",
			});
			return section;
		}
	}
}
