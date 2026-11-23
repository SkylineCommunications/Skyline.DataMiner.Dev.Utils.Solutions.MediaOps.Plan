namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;
	using Statuses = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Statuses;

	internal static class RecurringJobBehaviorDefinition
	{
		internal static DomBehaviorDefinition Create()
		{
			var definition = new DomBehaviorDefinition
			{
				ID = Ids.Behaviors.Recurringjob_Behavior.Id,
				Name = "RecurringJob_Behavior",
				InitialStatusId = Statuses.Active,
				Statuses = { new(Statuses.Active, "Active"), new(Statuses.Cancelled, "Cancelled"), new(Statuses.Completed, "Completed") },
				StatusTransitions = { new("active_to_completed", Statuses.Active, Statuses.Completed), new("active_to_cancelled", Statuses.Active, Statuses.Cancelled) },
			};
			// The legacy section-rule order differs from the status declaration order.
			foreach (var statusId in new[] { Statuses.Active, Statuses.Completed, Statuses.Cancelled })
			{
				definition.StatusSectionDefinitionLinks.Add(GetJobInfo(statusId));
				definition.StatusSectionDefinitionLinks.Add(GetMonitoringSettings(statusId));
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetJobExecution(statusId, visible: true));
				definition.StatusSectionDefinitionLinks.Add(GetNodes(statusId));
				definition.StatusSectionDefinitionLinks.Add(GetConnections(statusId));
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeRelationships(statusId));
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetErrors(statusId));
				definition.StatusSectionDefinitionLinks.Add(GetRecurringInfo(statusId));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetCostingAndBilling(status.Id));
			}

			foreach (var status in definition.Statuses)
			{
				definition.StatusSectionDefinitionLinks.Add(WorkflowBehaviorSectionDefinitions.GetNodeGroups(status.Id));
			}

			return definition;
		}

		private static DomStatusSectionDefinitionLink GetJobInfo(string statusId)
		{
			var inactive = statusId != Statuses.Active;
			return new(new(statusId, Ids.Sections.JobInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.JobInfo.JobID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobName) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobDescription) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.Workflow) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobStart) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobEnd) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobDuration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.ApproxOutDuration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.ApproxOutTime) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.JobPriority) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobSource) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobNotes) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.OriginalJobStart) { Visible = inactive, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.OriginalJobEnd) { Visible = inactive, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.JobConfirmation) { Visible = inactive, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.JobCancellation) { Visible = inactive, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.ActionNeeded) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.LockedBy) { Visible = false, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = inactive },
					new(Ids.Sections.JobInfo.Preroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.Postroll) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobInfo.JobSeriesID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
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
					new(Ids.Sections.MonitoringSettings.MonitoringServiceID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetNodes(string statusId)
		{
			return new(new(statusId, Ids.Sections.Nodes.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Nodes.NodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeAlias) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeType) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeReferenceID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeParentReferenceID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeIcon) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.AutomaticConfiguration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ConfigurationParameters) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.AdHocControlScript) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.CoreReservationNodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ReserveNode) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.Hidden) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeStartTime) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeEndTime) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.LinkedBookingIds) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectMode) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.ResourceSelectState) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.Billable) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfiguration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Nodes.NodeConfigurationStatus) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetConnections(string statusId)
		{
			return new(new(statusId, Ids.Sections.Connections.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Connections.ConnectionID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.SourceNodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.DestinationNodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionAlias) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionOrder) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionType) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionSubtype) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.PredefinedSubset) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionDetails) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.Connections.ConnectionExecutionScript) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		private static DomStatusSectionDefinitionLink GetRecurringInfo(string statusId)
		{
			var completed = statusId == Statuses.Completed;
			return new(new(statusId, Ids.Sections.RecurringInfo.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.RecurringInfo.Duration) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = completed },
					new(Ids.Sections.RecurringInfo.TimeZone) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = completed },
					new(Ids.Sections.RecurringInfo.RecurringPattern) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = completed },
					new(Ids.Sections.RecurringInfo.ProcessStatus) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = completed },
					new(Ids.Sections.RecurringInfo.DesiredJobStatus) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = completed },
				},
			};
		}
	}
}
