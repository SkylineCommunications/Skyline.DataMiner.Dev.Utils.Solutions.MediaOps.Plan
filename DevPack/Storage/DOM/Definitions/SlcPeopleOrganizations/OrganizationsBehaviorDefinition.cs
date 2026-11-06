namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations.SlcPeopleOrganizationsIds.Behaviors.Organizations;

	internal static class OrganizationsBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Statuses.Id,
				Name = "Organizations_Behavior",
				InitialStatusId = Statuses.Draft,
				Statuses = { new(Statuses.Draft, "Draft"), new(Statuses.Active, "Active"), new(Statuses.Deprecated, "Deprecated"), new(Statuses.Edit, "Edit") },
				StatusTransitions =
				{
					new("draft_to_active", Statuses.Draft, Statuses.Active),
					new("active_to_deprecated", Statuses.Active, Statuses.Deprecated),
					new("active_to_edit", Statuses.Active, Statuses.Edit),
					new("edit_to_active", Statuses.Edit, Statuses.Active) { FlowLevel = 1 },
				},
			};
			const string script = "People and Organizations - Behavior - Organization Behavior";
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
			var editable = statusId is Statuses.Draft or Statuses.Edit;
			var deprecated = statusId == Statuses.Deprecated;
			var active = statusId == Statuses.Active;
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.OrganizationInformation.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.OrganizationInformation.Name) { Visible = true, RequiredForStatus = editable, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.OrganizationInformation.Category) { Visible = true, RequiredForStatus = false, ReadOnly = deprecated, ClientReadOnly = active },
					new(Ids.Sections.OrganizationInformation.Icon) { Visible = false, RequiredForStatus = false, ReadOnly = statusId is Statuses.Draft or Statuses.Deprecated, ClientReadOnly = statusId is Statuses.Active or Statuses.Edit },
				},
			});
			definition.StatusSectionDefinitionLinks.Add(new(new(statusId, Ids.Sections.Contracts.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Contracts.Contract) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			});
		}
	}
}
