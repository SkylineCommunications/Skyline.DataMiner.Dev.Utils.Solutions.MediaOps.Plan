namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses;

	internal static class WorkflowBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Ids.Behaviors.Workflow_Behavior.Id,
				Name = "Workflow_behavior",
				InitialStatusId = Statuses.Draft,
				Statuses = { new(Statuses.Draft, "Draft"), new(Statuses.Complete, "Complete"), new(Statuses.Obsolete, "Obsolete") },
				StatusTransitions = { new("draft_to_complete", Statuses.Draft, Statuses.Complete) },
			};
			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(GetWorkflowInfo(status.Id));
				definition.StatusSectionDefinitionLinks.Add(GetWorkflowExecution(status.Id));
				definition.StatusSectionDefinitionLinks.Add(GetMonitoringSettings(status.Id));
				definition.StatusSectionDefinitionLinks.Add(GetNodes(status.Id));
				definition.StatusSectionDefinitionLinks.Add(GetConnections(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetCostingAndBilling(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeRelationships(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeGroups(status.Id));
			}

			return definition;
		}

		private static DomStatusSectionDefinitionLink GetWorkflowInfo(string statusId)
		{
			return new(new(statusId, Ids.Sections.WorkflowInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.WorkflowInfo.WorkflowName) { Visible = true, RequiredForStatus = statusId == Statuses.Draft, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.WorkflowDescription) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.Favorite) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.Priority) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.WorkflowNotes) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.LockedBy) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.Preroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.Postroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowInfo.JobType) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetWorkflowExecution(string statusId)
		{
			return new(new(statusId, Ids.Sections.WorkflowExecution.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.WorkflowExecution.WorkflowExecutionScript) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.WorkflowExecution.WorkflowConfiguration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetMonitoringSettings(string statusId)
		{
			return new(new(statusId, Ids.Sections.MonitoringSettings.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = true,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.MonitoringSettings.AtJobStart) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.MonitoringSettings.AtJobEnd) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.MonitoringSettings.MonitoringServiceTemplate) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.MonitoringSettings.MonitoringServiceID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetNodes(string statusId)
		{
			var obsolete = statusId == Statuses.Obsolete;
			var link = new DomStatusSectionDefinitionLink(new(statusId, Ids.Sections.Nodes.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Nodes.NodeID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.NodeAlias) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeType) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.NodeReferenceID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.NodeParentReferenceID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.NodeIcon) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.AutomaticConfiguration) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.ConfigurationParameters) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.AdHocControlScript) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.CoreReservationNodeID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.ReserveNode) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = obsolete },
					new(Ids.Sections.Nodes.Hidden) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeStartTime) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeEndTime) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.LinkedBookingIds) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectMode) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.Billable) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectState) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfiguration) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfigurationStatus) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
			if (obsolete)
			{
				// The original Obsolete rule places Hidden after Billable.
				var hidden = link.FieldDescriptorLinks[11];
				link.FieldDescriptorLinks.RemoveAt(11);
				link.FieldDescriptorLinks.Insert(16, hidden);
			}

			return link;
		}

		private static DomStatusSectionDefinitionLink GetConnections(string statusId)
		{
			var link = new DomStatusSectionDefinitionLink(new(statusId, Ids.Sections.Connections.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Connections.ConnectionID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.SourceNodeID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.DestinationNodeID) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionOrder) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionType) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionSubtype) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.PredefinedSubset) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionDetails) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionAlias) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionScript) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
			if (statusId == Statuses.Obsolete)
			{
				var alias = link.FieldDescriptorLinks[8];
				link.FieldDescriptorLinks.RemoveAt(8);
				link.FieldDescriptorLinks.Insert(3, alias);
			}

			return link;
		}
	}
}
