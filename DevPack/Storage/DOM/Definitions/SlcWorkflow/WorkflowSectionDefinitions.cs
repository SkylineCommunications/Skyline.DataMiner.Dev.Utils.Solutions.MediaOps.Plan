namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal static class WorkflowSectionDefinitions
	{
		internal static CustomSectionDefinition GetNodes()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Nodes.Id, Name = "Nodes" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeID, Name = "Node ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "An auto-assigned value representing the ID of this node within the job.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeAlias, Name = "Node alias", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Allows the user to provide a more user-friendly name for the node. ",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeType, Name = "Node type", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("Node Type", ("source", 0), ("source type", 1), ("resource", 2), ("resource pool", 3), ("destination", 4), ("destination type", 5), ("source instance", 6), ("destination instance", 7)),
				Tooltip = "Specifies the category or function of a node within a job.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeReferenceID, Name = "Node reference ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The DOM GUID of the resource, resource pool, or other entity that this node represents.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeParentReferenceID, Name = "Node parent reference ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The DOM GUID of the parent resource, resource pool, or other entity that this node represents.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeIcon, Name = "Node icon", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The name of the icon that will be used for the workflow visualization.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.AutomaticConfiguration, Name = "Automatic configuration?", FieldType = typeof(bool), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = "Indicates if the node needs to be configured when the job is run.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.ConfigurationParameters, Name = "Configuration parameters", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "A JSON object defining a list of configuration parameters and values for elements and virtual functions for user configuration.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.AdHocControlScript, Name = "Ad-hoc control script", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "An Automation script name that will be executed to configure this node.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.CoreReservationNodeID, Name = "Core Reservation Node ID", FieldType = typeof(long), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.ReserveNode, Name = "Reserve node?", FieldType = typeof(bool), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(true), Tooltip = "If checked, a booking will be created for this node to ensure resource availability. Leave unchecked if the node does not require a booking.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.Hidden, Name = "Hidden?", FieldType = typeof(bool), IsOptional = false, IsReadonly = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = "When marked TRUE, this node will be reserved as part of the booking but will not be displayed in the job or included in the calculation of rates or costs.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeStartTime, Name = "Node start time", FieldType = typeof(DateTime), IsOptional = true,
				Tooltip = "If different from the Job Start time, this indicates when the resource started being used.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeEndTime, Name = "Node end time", FieldType = typeof(DateTime), IsOptional = true,
				Tooltip = "\"If different from the Job End time, this indicates when the resource finished being used.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.LinkedBookingIds, Name = "Linked booking IDs", FieldType = typeof(string), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				Tooltip = "If the node resource has an associated SRM booking, this field stores the corresponding booking ID from SRM.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Nodes.ResourceSelectMode, Name = "Resource select mode", FieldType = typeof(GenericEnum<int>), IsOptional = true, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("Resource select mode", ("Manual", 0), ("Auto select at booking", 1), ("Auto select at runtime", 2)),
				Tooltip = "Indicates how and when the resource assignment is expected to be made.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Nodes.ResourceSelectState, Name = "Resource select state", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create("Resource select state", ("Selected", 0), ("Pending auto selection", 1), ("Pending manual selection", 2), ("Requested resource", 3), ("Error", 4)),
				Tooltip = "Indicates the current state of the resource selection process.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Nodes.Billable, Name = "Billable?", FieldType = typeof(bool), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<bool>(true), Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.Nodes.NodeConfiguration, Name = "Node configuration", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Configuration }, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Nodes.NodeConfigurationStatus, Name = "Node configuration status", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GetConfigurationStatusEnum(), Tooltip = String.Empty,
			});
			return section;
		}

		internal static CustomSectionDefinition GetConnections()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Connections.Id, Name = "Connections" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionID, Name = "Connection ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "An auto-assigned value representing the ID of this edge within the workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.SourceNodeID, Name = "Source node ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The ID of the node that is the source of this connection.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.DestinationNodeID, Name = "Destination node ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "The ID of the node that is the destination of this connection.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionAlias, Name = "Connection alias", FieldType = typeof(string), IsOptional = true,
				Tooltip = "An optional user-friendly name for the connection.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionExecutionOrder, Name = "Connection execution order", FieldType = typeof(long), IsOptional = true,
				Tooltip = "Indicates the order in which the connections will be made. ",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionType, Name = "Connection type", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("Level based", 0), ("Tag based", 1)),
				Tooltip = "Defines whether this connection is based on the levels or tags of its source and destination.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionSubtype, Name = "Connection subtype", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("All ", 0), ("Predefined subset", 1), ("Custom subset", 2), ("Shuffle", 3)),
				Tooltip = "Further specifies how the connection will be made. 'All': All matching levels or tags between the source and destination nodes will be connected. 'Predefined subset': Only valid for level-based connections. Only a predefined subset of matching levels between the source and destination nodes will be connected, for example, all audio levels. 'Custom subset': A freely defined subset of matching levels or tags will be connected, for example, level three to five. 'Shuffle': Levels or tags can be freely mapped from source to destination.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.Connections.PredefinedSubset, Name = "Predefined subset", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("V+A+D", 0), ("V", 1), ("A", 2), ("D", 3), ("V+A", 4), ("V+D", 5), ("A+D", 6)),
				Tooltip = "In the case of a level-based connection of subtype 'predefined subset,' this indicates the subset of levels to be used. This subset is a combination of all video levels ('V'), all audio levels ('A'), and/or all data levels ('D').",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionDetails, Name = "Connection details", FieldType = typeof(string), IsOptional = true,
				Tooltip = "For 'custom subset' or 'shuffle' connections, this field contains the exact mapping of levels or tags between the source and destination.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Connections.ConnectionExecutionScript, Name = "Connection execution script", FieldType = typeof(string), IsOptional = true,
				DefaultValue = new ValueWrapper<string>("Workflow.Connection.Default"),
				Tooltip = "Optionally specifies a script to be used when setting up this connection during the execution of the workflow or job.",
			});
			return section;
		}

		internal static CustomSectionDefinition GetNodeRelationships()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.NodeRelationships.Id, Name = "Node relationships" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.NodeRelationships.ParentNodeID, Name = "Parent node ID", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.NodeRelationships.ChildNodeID, Name = "Child node ID", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.NodeRelationships.RelationshipAction, Name = "Relationship action", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { Ids.Definitions.JobNodeRelationshipActions }, Tooltip = String.Empty,
			});
			return section;
		}

		internal static CustomSectionDefinition GetNodeGroups()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.NodeGroups.Id, Name = "Node groups" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.NodeGroups.GroupName, Name = "Group name", FieldType = typeof(string), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.NodeGroups.GroupNodeIds, Name = "Group node IDs", FieldType = typeof(List<string>), IsOptional = true, Tooltip = String.Empty,
			});
			return section;
		}

		internal static CustomSectionDefinition GetMonitoringSettings()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.MonitoringSettings.Id, Name = "Monitoring settings" };
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.MonitoringSettings.AtJobStart, Name = "At job start", FieldType = typeof(GenericEnum<int>), IsOptional = true, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("At Job Start", ("Create Service Immediately", 0), ("Create Service at Workflow Start", 1), ("Don't Create Service", 2)),
				Tooltip = "Determines whether and when a service will be created for this workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.MonitoringSettings.AtJobEnd, Name = "At job end", FieldType = typeof(GenericEnum<int>), IsOptional = true, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create("At Job End", ("Delete Service if One Exists", 0), ("Don't Delete Service", 1)),
				Tooltip = "Determines whether and when a service will be deleted for this workflow.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.MonitoringSettings.MonitoringServiceTemplate, Name = "Monitoring service template", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The name of the service template, which will be used to create a service when the workflow is run. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.MonitoringSettings.MonitoringServiceID, Name = "Monitoring service ID", FieldType = typeof(string), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				Tooltip = "The ID of the service associated to this job. ",
			});
			return section;
		}

		internal static CustomSectionDefinition GetErrors()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.Errors.Id, Name = "Errors" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Errors.ErrorCode, Name = "Error Code", FieldType = typeof(string), IsOptional = false,
				Tooltip = "The code of the error, which has to be unique for the linked object",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.Errors.ErrorMessage, Name = "Error Message", FieldType = typeof(string), IsOptional = false,
				Tooltip = "The message explaining why the error was raised",
			});
			return section;
		}

		internal static CustomSectionDefinition GetJobInfo()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobInfo.Id, Name = "Job info" };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobID, Name = "Job ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "A human-readable ID for a job, auto-generated for each job instance in a specific format, e.g. SLC_0001.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobName, Name = "Job name", FieldType = typeof(string), IsOptional = false,
				Tooltip = "A short name for the job. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobDescription, Name = "Job description", FieldType = typeof(string), IsOptional = true,
				Tooltip = "A description of the workflow, including its purpose, steps, and functionality.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.JobInfo.Workflow, Name = "Workflow", FieldType = typeof(Guid), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				DomDefinitionIds = { Ids.Definitions.Workflows }, Tooltip = "If the job was created starting from a workflow, this field links to that workflow object.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobStart, Name = "Job start", FieldType = typeof(DateTime), IsOptional = false, Tooltip = "The time the job starts. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobEnd, Name = "Job end", FieldType = typeof(DateTime), IsOptional = false, Tooltip = "The time the job ends. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobDuration, Name = "Job duration", FieldType = typeof(TimeSpan), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.ApproxOutDuration, Name = "Approx out duration", FieldType = typeof(TimeSpan), IsOptional = true, IsSoftDeleted = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.ApproxOutTime, Name = "Approx out time", FieldType = typeof(DateTime), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "An optional time that may be used at the end of a job if it exceeds the scheduled end time.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobPriority, Name = "Job priority", FieldType = typeof(GenericEnum<int>), IsOptional = false,
				DefaultValue = new ValueWrapper<int>((int)Ids.Enums.Jobpriority.Normal),
				GenericEnumInstance = GenericEnumFactory.Create("Job Priority", ("High", 0), ("Normal", 1), ("Low", 2)),
				Tooltip = "Describes the relative importance of the job. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobSource, Name = "Job source", FieldType = typeof(string), IsOptional = true, IsReadonly = true,
				Tooltip = "Tracks the source of a job, with potential values including user, API, element, etc.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobNotes, Name = "Job notes", FieldType = typeof(string), IsOptional = true,
				Tooltip = "A general note field for job-related communication.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.OriginalJobStart, Name = "Original job start", FieldType = typeof(DateTime), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The start date/time of the job at the time of job confirmation.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.OriginalJobEnd, Name = "Original job end", FieldType = typeof(DateTime), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The end date/time of the job at the time of job confirmation.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobConfirmation, Name = "Job confirmation", FieldType = typeof(DateTime), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The date/time the job was confirmed.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobCancellation, Name = "Job cancellation", FieldType = typeof(DateTime), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The date/time the job was canceled.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.ActionNeeded, Name = "Action needed", FieldType = typeof(bool), IsOptional = true,
				DefaultValue = new ValueWrapper<bool>(false), Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.LockedBy, Name = "Locked by", FieldType = typeof(string), IsOptional = true, IsHidden = true, IsSoftDeleted = true,
				Tooltip = "Indicates who locked this job.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.Preroll, Name = "Preroll", FieldType = typeof(DateTime), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.Postroll, Name = "Postroll", FieldType = typeof(DateTime), IsOptional = true, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobInfo.JobSeriesID, Name = "Job series ID", FieldType = typeof(string), IsOptional = true,
				Tooltip = "Holds DOM ID of the recurring job instance",
			});
			return section;
		}

		internal static CustomSectionDefinition GetJobExecution()
		{
			// The trailing space is part of the existing schema name.
			var section = new CustomSectionDefinition { ID = Ids.Sections.JobExecution.Id, Name = "Job execution " };
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.JobExecution.JobExecutionScript, Name = "Job execution script", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "A script that will run when the job starts.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.JobExecution.JobConfiguration, Name = "Job configuration", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { Ids.Definitions.Configuration }, Tooltip = String.Empty,
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.JobExecution.JobConfigurationStatus, Name = "Job configuration status", FieldType = typeof(GenericEnum<int>), IsOptional = true,
				GenericEnumInstance = GetConfigurationStatusEnum(), Tooltip = String.Empty,
			});
			return section;
		}

		internal static CustomSectionDefinition GetCostingAndBilling()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.CostingAndBilling.Id, Name = "Costing and billing" };
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)people_organizations")
			{
				ID = Ids.Sections.CostingAndBilling.Organization, Name = "Organization", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("c769a960-e401-4cb6-8099-b0563b270c52")) { ModuleId = "(slc)people_organizations" } },
				Tooltip = "The organization that owns this job or for which the job is being performed. The organization determines which contacts and contracts can be assigned to the job.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)people_organizations")
			{
				ID = Ids.Sections.CostingAndBilling.JobOwner, Name = "Job owner", FieldType = typeof(Guid), IsOptional = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("1624a01d-de2e-4c36-849c-3ee048a971ee")) { ModuleId = "(slc)people_organizations" } },
				Tooltip = "The main contact for this job. ",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)people_organizations")
			{
				ID = Ids.Sections.CostingAndBilling.AdditionalContacts, Name = "Additional contacts", FieldType = typeof(List<Guid>), IsOptional = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("1624a01d-de2e-4c36-849c-3ee048a971ee")) { ModuleId = "(slc)people_organizations" } },
				Tooltip = "Contacts associated with this particular job.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = Ids.Sections.CostingAndBilling.Contract, Name = "Contract", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("29c0ccd4-6714-423d-9c2e-805b9dfc3abd")) { ModuleId = "(slc)cost_billing" } },
				Tooltip = "Indicates the Contract used to calculate the Billing Rates and Costs for the Job. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.JobStatus, Name = "Job status", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The status of the job when cost or billing calculation started.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.ContractUplift, Name = "Contract uplift (%)", FieldType = typeof(double), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<double>(0), Tooltip = "Tracks the percentage by which a job's rate will be increased, if any.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.ContractDiscount, Name = "Contract discount (%)", FieldType = typeof(double), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<double>(0), Tooltip = "Tracks the percentage by which the job is discounted, if any.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.SpeedOrderIncrementOnTopOfBillingPrice, Name = "Speed order increment on top of billing price (%)", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The percentage added to the billing price if the job starts earlier than scheduled.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.SpeedOrderFixedFee, Name = "Speed order fixed fee", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "If a job starts earlier than scheduled, a fixed fee will be added to the existing percentage fee calculated on the total billing price.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.CancellationOfBillingPrice, Name = "Cancellation % of billing price", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "Percentage of the billing price to be charged due to job cancellation.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.CancellationFixedFee, Name = "Cancellation fixed fee", FieldType = typeof(string), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "If a job was canceled, a fixed fee will be added to the existing percentage fee calculated on the total billing price.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.LastBillCalculation, Name = "Last bill calculation", FieldType = typeof(DateTime), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				Tooltip = "The date and time of the last calculation of costs and pricing. ",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.TotalBillNetAmount, Name = "Total bill net amount", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "The total bill amount after fees.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBilling.TotalBillNetOverrideAmount, Name = "Total bill net override amount", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				Tooltip = "An override value for the total bill net amount.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = Ids.Sections.CostingAndBilling.BillCurrency, Name = "Bill currency", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("b5872cf0-ee4f-466a-a963-358bf1ed190c")) { ModuleId = "(slc)cost_billing" } },
				Tooltip = "The currency used for rate calculations. By default, the job's currency is set by the selected contract. READ ONLY.",
			});
			return section;
		}

		private static GenericEnum<int> GetConfigurationStatusEnum()
		{
			return GenericEnumFactory.Create(String.Empty, ("No values needed", 0), ("Mandatory values missing", 1), ("Non-mandatory values missing", 2), ("All values provided", 3), ("No parameters defined", 4));
		}
	}
}
