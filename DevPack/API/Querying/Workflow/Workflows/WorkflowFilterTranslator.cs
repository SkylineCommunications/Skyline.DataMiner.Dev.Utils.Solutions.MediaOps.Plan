namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal class WorkflowFilterTranslator : DomInstanceFilterTranslator<Workflow>
	{
		private readonly FilterElement<DomInstance> workflowsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id)
			.AND(DomInstanceExposers.StatusId.NotEqual(SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.Obsolete));
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[WorkflowExposers.Id.fieldName] = HandleGuid,
			[WorkflowExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowName), comparer, (string)value),
			[WorkflowExposers.Description.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowDescription), comparer, (string)value),
			[WorkflowExposers.Notes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowNotes), comparer, (string)value),
			[WorkflowExposers.IsFavorite.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Favorite), comparer, (bool)value),
			[WorkflowExposers.Priority.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Priority), comparer, ConvertWorkflowPriority((WorkflowPriority)value)),
			[WorkflowExposers.PreRoll.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Preroll), comparer, (TimeSpan)value),
			[WorkflowExposers.PostRoll.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Postroll), comparer, (TimeSpan)value),
			[WorkflowExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertWorkflowState((WorkflowState)value)),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => workflowsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		private static int ConvertWorkflowPriority(WorkflowPriority priority)
		{
			switch (priority)
			{
				case WorkflowPriority.High:
					return (int)SlcWorkflowIds.Enums.Priority.High;
				case WorkflowPriority.Normal:
					return (int)SlcWorkflowIds.Enums.Priority.Normal;
				case WorkflowPriority.Low:
					return (int)SlcWorkflowIds.Enums.Priority.Low;
				default:
					throw new InvalidOperationException($"Unsupported workflow priority: {priority}");
			}
		}

		private static string ConvertWorkflowState(WorkflowState state)
		{
			switch (state)
			{
				case WorkflowState.Draft:
					return SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.Draft;
				case WorkflowState.Complete:
					return SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.Complete;
				default:
					throw new InvalidOperationException($"Unsupported workflow state: {state}");
			}
		}
	}
}