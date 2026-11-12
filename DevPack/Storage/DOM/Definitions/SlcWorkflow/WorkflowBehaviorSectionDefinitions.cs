namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow
{
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;

	using Ids = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds;

	internal static class WorkflowBehaviorSectionDefinitions
	{
		internal static DomStatusSectionDefinitionLink GetCostingAndBilling(string statusId)
		{
			return new(new(statusId, Ids.Sections.CostingAndBilling.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.CostingAndBilling.Organization) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.JobOwner) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.AdditionalContacts) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.Contract) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.JobStatus) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.ContractUplift) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.ContractDiscount) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.SpeedOrderIncrementOnTopOfBillingPrice) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.SpeedOrderFixedFee) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.CancellationOfBillingPrice) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.CancellationFixedFee) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.LastBillCalculation) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.TotalBillNetAmount) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.TotalBillNetOverrideAmount) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.CostingAndBilling.BillCurrency) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		internal static DomStatusSectionDefinitionLink GetNodeRelationships(string statusId)
		{
			return new(new(statusId, Ids.Sections.NodeRelationships.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.NodeRelationships.ParentNodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.NodeRelationships.ChildNodeID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.NodeRelationships.RelationshipAction) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}

		internal static DomStatusSectionDefinitionLink GetErrors(string statusId)
		{
			return new(new(statusId, Ids.Sections.Errors.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.Errors.ErrorCode) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
					new(Ids.Sections.Errors.ErrorMessage) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			};
		}

		internal static DomStatusSectionDefinitionLink GetNodeGroups(string statusId)
		{
			return new(new(statusId, Ids.Sections.NodeGroups.Id))
			{
				AllowMultipleSections = true,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.NodeGroups.GroupName) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.NodeGroups.GroupNodeIds) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.NodeGroups.GroupID) { Visible = true, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = true },
				},
			};
		}

		internal static DomStatusSectionDefinitionLink GetJobExecution(string statusId, bool visible)
		{
			return new(new(statusId, Ids.Sections.JobExecution.Id))
			{
				AllowMultipleSections = false,
				IsSoftDeleted = false,
				FieldDescriptorLinks = new()
				{
					new(Ids.Sections.JobExecution.JobExecutionScript) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobExecution.JobConfiguration) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
					new(Ids.Sections.JobExecution.JobConfigurationStatus) { Visible = visible, RequiredForStatus = false, ReadOnly = false, ClientReadOnly = false },
				},
			};
		}
	}
}
