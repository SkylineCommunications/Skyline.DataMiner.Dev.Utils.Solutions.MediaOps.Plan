namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds.Behaviors.People;

	internal static class PeopleBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Statuses.Id,
				Name = "People_Behavior",
				InitialStatusId = Statuses.Draft,
				Statuses = { new(Statuses.Draft, "Draft"), new(Statuses.Active, "Active"), new(Statuses.Deprecated, "Deprecated"), new(Statuses.Edit, "Edit") },
				StatusTransitions =
				{
					new("draft_to_active", Statuses.Draft, Statuses.Active),
					new("active_to_deprecated", Statuses.Active, Statuses.Deprecated),
					new("active_to_edit", Statuses.Active, Statuses.Edit),
					new("edit_to_active", Statuses.Edit, Statuses.Active),
				},
			};
			const string script = "People and Organizations - Behavior - People Behavior";
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "confirm", "Confirm", "Activate", Statuses.Draft, Statuses.Edit);
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "archive", "Archive", "Deprecate", Statuses.Active);
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "edit", "Edit", "Edit", Statuses.Active);
			foreach (var status in definition.Statuses)
			{
				AddPeopleContactAndMembership(definition, status.Id);
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(new(new(status.Id, Ids.Sections.Resource.Id))
				{
					AllowMultipleSections = false,
					IsSoftDeleted = false,
					FieldDescriptorLinks = new()
					{
						new(Ids.Sections.Resource.LinkedResource) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					},
				});
			}

			return definition;
		}

		private static void AddPeopleContactAndMembership(DomBehaviorDefinition definition, string statusId)
		{
			var editable = statusId is Statuses.Draft or Statuses.Edit;
			var deprecated = statusId == Statuses.Deprecated;
			var active = statusId == Statuses.Active;
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.PeopleInformation.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.PeopleInformation.FullName) { Visible = true, RequiredForStatus = editable, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.PeopleInformation.Skills) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = !deprecated },
					new(Ids.Sections.PeopleInformation.ExperienceLevel) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.PeopleInformation.ProfileImage) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ContactInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ContactInfo.Email) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.ContactInfo.Phone) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.ContactInfo.StreetAddress) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.ContactInfo.City) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.ContactInfo.Zip) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.ContactInfo.Country) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.Organization.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Organization.OrganizationReference) { Visible = statusId == Statuses.Draft, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = false },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.Team.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Team.TeamReference) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Team.TeamRole) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}
	}
}
