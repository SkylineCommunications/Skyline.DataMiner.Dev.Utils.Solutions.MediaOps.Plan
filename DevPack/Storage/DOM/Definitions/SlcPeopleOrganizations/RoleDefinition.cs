namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	internal sealed class RoleDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Role")
		{
			ID = SlcPeopleOrganizationsIds.Definitions.Role,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(SlcPeopleOrganizationsIds.Sections.RoleInformation.Role) } },
			},
			SectionDefinitionLinks = { new SectionDefinitionLink(SlcPeopleOrganizationsIds.Sections.RoleInformation.Id) { IsOptional = false, AllowMultipleSections = false } },
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetRoleInformation() };

		private static CustomSectionDefinition GetRoleInformation()
		{
			var section = new CustomSectionDefinition { ID = SlcPeopleOrganizationsIds.Sections.RoleInformation.Id, Name = "Role information" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = SlcPeopleOrganizationsIds.Sections.RoleInformation.Role, Name = "Role", FieldType = typeof(string), IsOptional = false,
				Tooltip = "The role a contact assumes when assigned to a team.",
			});
			return section;
		}
	}
}
