namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds.Behaviors.Teams;

	internal static class TeamsBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Statuses.Id,
				Name = "Team_Behavior",
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
			const string script = "People and Organizations - Behavior - Team Behavior";
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "confirm", "Confirm", "Activate", Statuses.Draft, Statuses.Edit);
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "edit", "Edit", "Edit", Statuses.Active);
			PeopleOrganizationsBehaviorTools.AddActionAndButton(definition, script, "archive", "Archive", "Deprecate", Statuses.Active);
			foreach (var status in definition.Statuses)
			{
				AddStatusSections(definition, status.Id);
			}

			return definition;
		}

		private static void AddStatusSections(DomBehaviorDefinition definition, string statusId)
		{
			var deprecated = statusId == Statuses.Deprecated;
			var active = statusId == Statuses.Active;
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.TeamInformation.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.TeamInformation.Name) { Visible = true, RequiredForStatus = !deprecated, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.TeamInformation.Email) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.TeamInformation.Description) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.TeamInformation.Skills) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.TeamInformation.Bookable) { Visible = deprecated, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = false },
					new(Ids.Sections.TeamInformation.Icon) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.ResourcePool.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.ResourcePool.LinkedResourcePool) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}
	}
}
