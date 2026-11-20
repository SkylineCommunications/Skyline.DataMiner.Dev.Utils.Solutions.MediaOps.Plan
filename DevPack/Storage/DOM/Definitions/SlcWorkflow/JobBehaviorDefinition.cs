namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Buttons;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Conditions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;
	using Skyline.DataMiner.Net.Sections;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.Statuses;

	internal static class JobBehaviorDefinition
	{
		// Referenced by the legacy behavior but absent from its section schema.
		// Preserve the reference; this is not a declaration of a new descriptor.
		private static readonly FieldDescriptorID UnmodeledMonitoringFieldId = new(new Guid("a6649369-2c5c-4b84-a745-6aa94ca8d550"));

		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Ids.Behaviors.Job_Behavior.Id,
				Name = "Job_Behavior",
				InitialStatusId = Statuses.Draft,
				Statuses =
				{
					new(Statuses.Draft, "Draft"), new(Statuses.Tentative, "Tentative"), new(Statuses.Confirmed, "Confirmed"), new(Statuses.Running, "Running"),
					new(Statuses.Completed, "Completed"), new(Statuses.ReadyForInvoice, "Ready For Invoice"), new(Statuses.Canceled, "Canceled"), new(Statuses.Invoiced, "Invoiced"),
				},
				StatusTransitions =
				{
					new("draft_to_tentative", Statuses.Draft, Statuses.Tentative),
					new("tentative_to_confirmed", Statuses.Tentative, Statuses.Confirmed),
					new("confirmed_to_running", Statuses.Confirmed, Statuses.Running),
					new("running_to_completed", Statuses.Running, Statuses.Completed),
					new("confirmed_to_canceled", Statuses.Confirmed, Statuses.Canceled) { FlowLevel = 1 },
					new("tentative_to_canceled", Statuses.Tentative, Statuses.Canceled) { FlowLevel = 1 },
					new("draft_to_completed", Statuses.Draft, Statuses.Completed) { FlowLevel = 2 },
					new("tentative_to_completed", Statuses.Tentative, Statuses.Completed) { FlowLevel = 2 },
					new("confirmed_to_tentative", Statuses.Confirmed, Statuses.Tentative) { FlowLevel = 2 },
				},
			};
			AddButton(definition, "confirm job", "Confirm job", "primary", Statuses.Tentative);
			AddButton(definition, "manual start", "Manual start", String.Empty, Statuses.Confirmed);
			AddButton(definition, "stop early", "Stop early", String.Empty, Statuses.Running);
			AddButton(definition, "cancel job", "Cancel job", String.Empty, Statuses.Tentative, Statuses.Confirmed);
			AddButton(definition, "save as tentative", "Save as Tentative", "primary", Statuses.Draft);
			AddButton(definition, "edit job config", "Edit job config", String.Empty, Statuses.Draft, Statuses.Tentative, Statuses.Confirmed);
			AddButton(definition, "return to tentative", "Return to Tentative", String.Empty, Statuses.Confirmed);

			// Preserve the original section-rule ordering, not just status order.
			foreach (var statusId in new[] { Statuses.Draft, Statuses.Tentative, Statuses.Confirmed, Statuses.Completed, Statuses.Canceled, Statuses.ReadyForInvoice, Statuses.Running, Statuses.Invoiced })
			{
				definition.StatusSectionDefinitionLinks.Add(GetJobInfo(statusId));
				definition.StatusSectionDefinitionLinks.Add(GetMonitoringSettings(statusId));
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetJobExecution(statusId, visible: statusId != Statuses.Draft));
				definition.StatusSectionDefinitionLinks.Add(GetNodes(statusId));
				definition.StatusSectionDefinitionLinks.Add(GetConnections(statusId));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetCostingAndBilling(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(GetCostingAndBillingDetails(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeRelationships(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetErrors(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeGroups(status.Id));
			}

			return definition;
		}

		private static void AddButton(DomBehaviorDefinition definition, string id, string text, string tooltip, params string[] statuses)
		{
			definition.ButtonDefinitions.Add(new DomInstanceButtonDefinition(id)
			{
				ActionDefinitionIds = new List<string>(),
				Layout = new() { Text = text, ToolTip = tooltip, Icon = String.Empty, Order = 0 },
				VisibilityCondition = new StatusCondition(new List<string>(statuses)),
			});
		}

		private static DomStatusSectionDefinitionLink GetJobInfo(string statusId)
		{
			var draft = statusId == Statuses.Draft;
			var invoicing = statusId == Statuses.ReadyForInvoice;
			var originalDatesVisible = statusId is not (Statuses.Confirmed or Statuses.Running);
			var originalDatesClientReadOnly = originalDatesVisible && !invoicing;
			return new(new(statusId, Ids.Sections.JobInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.JobInfo.JobID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobName) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobDescription) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.Workflow) { Visible = !draft, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = draft },
					new(Ids.Sections.JobInfo.JobStart) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobEnd) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobDuration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.ApproxOutDuration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.ApproxOutTime) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.JobInfo.JobPriority) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = invoicing },
					new(Ids.Sections.JobInfo.JobSource) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = invoicing },
					new(Ids.Sections.JobInfo.JobNotes) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = invoicing },
					new(Ids.Sections.JobInfo.OriginalJobStart) { Visible = originalDatesVisible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = originalDatesClientReadOnly },
					new(Ids.Sections.JobInfo.OriginalJobEnd) { Visible = originalDatesVisible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = originalDatesClientReadOnly },
					new(Ids.Sections.JobInfo.JobConfirmation) { Visible = originalDatesVisible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = originalDatesClientReadOnly },
					new(Ids.Sections.JobInfo.JobCancellation) { Visible = originalDatesVisible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = originalDatesClientReadOnly },
					// Deliberate duplicate from the legacy behavior; Running has a different visibility here.
					new(Ids.Sections.JobInfo.JobID) { Visible = statusId != Statuses.Running, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.ActionNeeded) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.LockedBy) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.JobInfo.Preroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.Postroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobSeriesID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetMonitoringSettings(string statusId)
		{
			var visible = statusId != Statuses.Draft;
			var link = new DomStatusSectionDefinitionLink(new(statusId, Ids.Sections.MonitoringSettings.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.MonitoringSettings.AtJobStart) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.MonitoringSettings.AtJobEnd) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.MonitoringSettings.MonitoringServiceTemplate) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
			if (statusId is Statuses.Confirmed or Statuses.Canceled or Statuses.Running)
			{
				link.FieldDescriptorLinks.Add(new(UnmodeledMonitoringFieldId) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false });
			}
			else if (statusId != Statuses.Invoiced)
			{
				// Preserve the source's duplicate instead of normalizing behavior rules.
				link.FieldDescriptorLinks.Add(new(Ids.Sections.MonitoringSettings.AtJobStart) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false });
			}

			link.FieldDescriptorLinks.Add(new(Ids.Sections.MonitoringSettings.MonitoringServiceID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false });
			return link;
		}

		private static DomStatusSectionDefinitionLink GetNodes(string statusId)
		{
			var visible = statusId != Statuses.Draft;
			var billableVisible = statusId is Statuses.Confirmed or Statuses.Completed or Statuses.Canceled;
			return new(new(statusId, Ids.Sections.Nodes.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Nodes.NodeID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeAlias) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeType) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeReferenceID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeParentReferenceID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeIcon) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.AutomaticConfiguration) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ConfigurationParameters) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.AdHocControlScript) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.CoreReservationNodeID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ReserveNode) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.Hidden) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeStartTime) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeEndTime) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.LinkedBookingIds) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectMode) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectState) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.Billable) { Visible = billableVisible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfiguration) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfigurationStatus) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetConnections(string statusId)
		{
			var visible = statusId != Statuses.Draft;
			var link = new DomStatusSectionDefinitionLink(new(statusId, Ids.Sections.Connections.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Connections.ConnectionID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.SourceNodeID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.DestinationNodeID) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionOrder) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionType) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionSubtype) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.PredefinedSubset) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionDetails) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionScript) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
			var aliasIndex = statusId switch { Statuses.Tentative => 1, Statuses.Draft or Statuses.Invoiced => 3, _ => 8 };
			link.FieldDescriptorLinks.Insert(aliasIndex, new(Ids.Sections.Connections.ConnectionAlias) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false });
			return link;
		}

		private static DomStatusSectionDefinitionLink GetCostingAndBillingDetails(string statusId)
		{
			return new(new(statusId, Ids.Sections.CostingAndBillingDetails.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.CostingAndBillingDetails.LineItemType) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.ObjectType) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.ObjectID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.Description) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.RateCard) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.TotalNetOverrideAmount) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.Currency) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBillingDetails.Rates) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}
	}
}
