namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;

	internal sealed class OrganizationsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Organizations")
		{
			ID = Ids.Definitions.Organizations,
			DomBehaviorDefinitionId = Ids.Behaviors.Organizations.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.OrganizationInformation.Name) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.OrganizationInformation.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.Contracts.Id) { IsOptional = true, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetOrganizationInformation(), GetContracts() };

		private static CustomSectionDefinition GetOrganizationInformation()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.OrganizationInformation.Id, Name = "Organization information" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrganizationInformation.Name, Name = "Organization name", FieldType = typeof(string), IsOptional = false, Tooltip = "The name of this organization.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.OrganizationInformation.Category, Name = "Category", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Category }, Tooltip = "For example, Software Company, Social Media, Production Studio, etc.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.OrganizationInformation.Icon, Name = "Icon", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The relative qualified path to the organization's icon. This is a backend field descriptor.",
			});
			return section;
		}

		private static CustomSectionDefinition GetContracts()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Contracts.Id, Name = "Contracts" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = Ids.Sections.Contracts.Contract, Name = "Contract", FieldType = typeof(Guid), IsOptional = true, Tooltip = String.Empty,
				DomDefinitionIds = { new DomDefinitionId(new Guid("29c0ccd4-6714-423d-9c2e-805b9dfc3abd")) { ModuleId = "(slc)cost_billing" } },
			});
			return section;
		}
	}
}
