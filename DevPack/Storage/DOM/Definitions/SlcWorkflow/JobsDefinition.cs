namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Settings;
	using Skyline.DataMiner.Net.Apps.Sections.SectionDefinitions;
	using Skyline.DataMiner.Net.GenericEnums;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Interfaces;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal sealed class JobsDefinition : IDomDefinitionInfo
	{
		public DomDefinition Definition { get; } = new DomDefinition("Jobs")
		{
			ID = Ids.Definitions.Jobs,
			DomBehaviorDefinitionId = Ids.Behaviors.Job_Behavior.Id,
#pragma warning disable CS0612, CS0618 // Preserve the deployed schema's legacy visual metadata.
			VisualStructure = new() { IsHidden = false, SectionDefinitionInfos = new() },
#pragma warning restore CS0612, CS0618
			ModuleSettingsOverrides = new ModuleSettingsOverrides
			{
				NameDefinition = new DomInstanceNameDefinition { ConcatenationItems = { new FieldValueConcatenationItem(Ids.Sections.JobInfo.JobName) } },
				ScriptSettings = new ExecuteScriptOnDomInstanceActionSettings
				{
					OnCreate = "WorkflowDesigner_CRUD_Workflow", OnDelete = "WorkflowDesigner_CRUD_Workflow", OnUpdate = "WorkflowDesigner_CRUD_Workflow",
					ScriptType = OnDomInstanceActionScriptType.FullCrudMeta,
				},
			},
			SectionDefinitionLinks =
			{
				new SectionDefinitionLink(Ids.Sections.JobInfo.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.MonitoringSettings.Id) { IsOptional = false, AllowMultipleSections = false, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.JobExecution.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.Nodes.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Connections.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.CostingAndBilling.Id) { IsOptional = false, AllowMultipleSections = false },
				new SectionDefinitionLink(Ids.Sections.CostingAndBillingDetails.Id) { IsOptional = false, AllowMultipleSections = true, IsSoftDeleted = true },
				new SectionDefinitionLink(Ids.Sections.NodeRelationships.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.Errors.Id) { IsOptional = false, AllowMultipleSections = true },
				new SectionDefinitionLink(Ids.Sections.NodeGroups.Id) { IsOptional = false, AllowMultipleSections = true },
			},
		};

		public IEnumerable<CustomSectionDefinition> SectionDefinitions { get; } = new[]
		{
			WorkflowSectionDefinitions.GetJobInfo(), WorkflowSectionDefinitions.GetMonitoringSettings(), WorkflowSectionDefinitions.GetJobExecution(),
			WorkflowSectionDefinitions.GetNodes(), WorkflowSectionDefinitions.GetConnections(), WorkflowSectionDefinitions.GetCostingAndBilling(), GetCostingAndBillingDetails(),
			WorkflowSectionDefinitions.GetNodeRelationships(), WorkflowSectionDefinitions.GetErrors(), WorkflowSectionDefinitions.GetNodeGroups(),
		};

		private static CustomSectionDefinition GetCostingAndBillingDetails()
		{
			var section = new CustomSectionDefinition { ID = Ids.Sections.CostingAndBillingDetails.Id, Name = "Costing and billing details " };
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.CostingAndBillingDetails.LineItemType, Name = "Line item type", FieldType = typeof(GenericEnum<string>), IsOptional = true, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("Costing", Ids.Enums.Lineitemtype.Costing), ("Billing", Ids.Enums.Lineitemtype.Billing)),
				Tooltip = "Determines whether this record is a billing or costing record.",
			});
			section.AddOrReplaceFieldDescriptor(new GenericEnumFieldDescriptor
			{
				ID = Ids.Sections.CostingAndBillingDetails.ObjectType, Name = "Object type", FieldType = typeof(GenericEnum<string>), IsOptional = true, IsSoftDeleted = true,
				GenericEnumInstance = GenericEnumFactory.Create(String.Empty, ("Node", Ids.Enums.Objecttype.Node), ("Workflow", Ids.Enums.Objecttype.Workflow)),
				Tooltip = "The type of object for which cost or billing is being calculated (node or workflow).",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBillingDetails.ObjectID, Name = "Object ID", FieldType = typeof(string), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				Tooltip = "Captures the object ID for which the cost & billing apply. Depending on the object type, this can be the ID of either a workflow or a node.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBillingDetails.Description, Name = "Description ", FieldType = typeof(string), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				Tooltip = "A system field describing the cost and billing entry, such as the name of the node or the workflow that generated it.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = Ids.Sections.CostingAndBillingDetails.RateCard, Name = "Rate card", FieldType = typeof(Guid), IsOptional = true, IsReadonly = true, IsSoftDeleted = true,
				DomDefinitionIds =
				{
					new DomDefinitionId(new Guid("0bd66725-17c9-4a8d-aabc-60da64995113")) { ModuleId = "(slc)cost_billing" },
					new DomDefinitionId(new Guid("dd02ad2b-460c-42b1-b9f2-9f6c391ca72f")) { ModuleId = "(slc)cost_billing" },
				},
				Tooltip = "The rate card associated with the node or workflow used to calculate the total amount.",
			});
			section.AddOrReplaceFieldDescriptor(new FieldDescriptor
			{
				ID = Ids.Sections.CostingAndBillingDetails.TotalNetOverrideAmount, Name = "Total net override amount", FieldType = typeof(double), IsOptional = true, IsSoftDeleted = true,
				DefaultValue = new ValueWrapper<double>(0), Tooltip = "Allows a user override for the total net amount and is the value used by the system to calculate the job's total rate.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor("(slc)cost_billing")
			{
				ID = Ids.Sections.CostingAndBillingDetails.Currency, Name = "Currency", FieldType = typeof(Guid), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { new DomDefinitionId(new Guid("b5872cf0-ee4f-466a-a963-358bf1ed190c")) { ModuleId = "(slc)cost_billing" } },
				Tooltip = "The currency in which the total net amount or its override will be expressed. When billing, this currency is derived from the contract. When calculating cost, this currency is derived from the rate card in use.",
			});
			section.AddOrReplaceFieldDescriptor(new DomInstanceFieldDescriptor(Ids.ModuleId)
			{
				ID = Ids.Sections.CostingAndBillingDetails.Rates, Name = "Rates", FieldType = typeof(List<Guid>), IsOptional = true, IsSoftDeleted = true,
				DomDefinitionIds = { Ids.Definitions.Rates }, Tooltip = "The rates used to charge the current workflow or node, including the total amount and total net amount after discounts.",
			});
			return section;
		}
	}
}
