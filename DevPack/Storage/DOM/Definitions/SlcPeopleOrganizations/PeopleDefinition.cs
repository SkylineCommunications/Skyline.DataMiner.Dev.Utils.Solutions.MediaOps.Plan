namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;

	internal sealed class PeopleDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("People")
		{
			ID = Ids.Definitions.People,
			DomBehaviorDefinitionId = Ids.Behaviors.People.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.PeopleInformation.FullName) } },
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.PeopleInformation.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.ContactInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.Organization.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.Team.Id) { IsOptional = true, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Resource.Id) { IsOptional = false, AllowMultipleSections = false },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[] { GetPeopleInformation(), GetContactInfo(), GetOrganization(), GetTeam(), GetResource() };

		private static CustomSectionDefinition GetPeopleInformation()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.PeopleInformation.Id, Name = "People information" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.PeopleInformation.FullName, Name = "Full name", FieldType = typeof(string), IsOptional = false,
				Tooltip = "This contact's full name.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.PeopleInformation.ExperienceLevel, Name = "Experience level", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Experience }, Tooltip = "For example, Junior, Senior, Manager, etc.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.PeopleInformation.ProfileImage, Name = "Profile Image", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.PeopleInformation.Skills, Name = "Personal skills", FieldType = typeof(string), IsOptional = false,
				Tooltip = "For example, Producer, Director, Video Editor, Audio Mixer, etc.",
			});
			return section;
		}

		private static CustomSectionDefinition GetContactInfo()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.ContactInfo.Id, Name = "Contact info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.Email, Name = "Email", FieldType = typeof(string), IsOptional = true, Tooltip = "This contact's email address.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.Phone, Name = "Phone", FieldType = typeof(string), IsOptional = true, Tooltip = "This contact's phone number.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.StreetAddress, Name = "Street address", FieldType = typeof(string), IsOptional = true, Tooltip = "This contact's current street address of residence.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.City, Name = "City", FieldType = typeof(string), IsOptional = true, Tooltip = "This contact's current city of residence.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.Country, Name = "Country", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = CountryEnum.Create(), Tooltip = "This contact's current country of residence.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.ContactInfo.Zip, Name = "ZIP", FieldType = typeof(string), IsOptional = true, Tooltip = "This contact's current ZIP code.",
			});
			return section;
		}

		private static CustomSectionDefinition GetOrganization()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Organization.Id, Name = "Organization" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.Organization.OrganizationReference, Name = "Organization", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Organizations }, Tooltip = "This contact's current organization.",
			});
			return section;
		}

		private static CustomSectionDefinition GetTeam()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Team.Id, Name = "Team" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.Team.TeamReference, Name = "Team", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Teams }, Tooltip = "This contact's team membership.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.Team.TeamRole, Name = "Team role", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Role }, Tooltip = "This contact's current role in the specified team.",
			});
			return section;
		}

		private static CustomSectionDefinition GetResource()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Resource.Id, Name = "Resource" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)resource_studio")
			{
				ID = Ids.Sections.Resource.LinkedResource, Name = "Linked resource", FieldType = typeof(Guid), IsOptional = true, Tooltip = String.Empty,
				DomDefinitionIds = { new DomDefinitionId(new Guid("d2afc1dc-f39c-49c8-a70f-9120bfbfc0a0")) { ModuleId = "(slc)resource_studio" } },
			});
			return section;
		}
	}
}
