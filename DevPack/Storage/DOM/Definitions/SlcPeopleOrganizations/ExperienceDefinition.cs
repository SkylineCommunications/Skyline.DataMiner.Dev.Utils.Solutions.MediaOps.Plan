namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	internal sealed class ExperienceDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Experience")
		{
			ID = SlcPeopleOrganizationsIds.Definitions.Experience,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(SlcPeopleOrganizationsIds.Sections.ExperienceInformation.Experience) } },
			},
			SectionDefinitionLinks = { new SectionDefinitionLink(SlcPeopleOrganizationsIds.Sections.ExperienceInformation.Id) { IsOptional = false, AllowMultipleSections = false } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetExperienceInformation() };

		private static CustomSectionDefinition GetExperienceInformation()
		{
			var section = new CustomSectionDefinition { ID = SlcPeopleOrganizationsIds.Sections.ExperienceInformation.Id, Name = "Experience information" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPeopleOrganizationsIds.Sections.ExperienceInformation.Experience, Name = "Experience", FieldType = typeof(string), IsOptional = false,
				Tooltip = "For example, Junior, Senior, Manager, etc.",
			});
			return section;
		}
	}
}
