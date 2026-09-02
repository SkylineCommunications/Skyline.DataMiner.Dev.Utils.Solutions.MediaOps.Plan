namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using SLDataGateway.API.Types.Querying;

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
			[WorkflowExposers.JobTypeCategoryId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.JobType), comparer, (string)value),
			[WorkflowExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertWorkflowState((WorkflowState)value)),
		};

		private readonly Dictionary<string, Func<SortOrder, bool, IOrderByElement>> orderByHandlers = new Dictionary<string, Func<SortOrder, bool, IOrderByElement>>
		{
			[WorkflowExposers.Id.fieldName] = HandleGuid,
			[WorkflowExposers.Name.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowName), sortOrder, naturalSort),
			[WorkflowExposers.Description.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowDescription), sortOrder, naturalSort),
			[WorkflowExposers.Notes.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.WorkflowNotes), sortOrder, naturalSort),
			[WorkflowExposers.IsFavorite.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Favorite), sortOrder, naturalSort),
			[WorkflowExposers.Priority.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Priority), sortOrder, naturalSort),
			[WorkflowExposers.PreRoll.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Preroll), sortOrder, naturalSort),
			[WorkflowExposers.PostRoll.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.Postroll), sortOrder, naturalSort),
			[WorkflowExposers.JobTypeCategoryId.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowInfo.JobType), sortOrder, naturalSort),
			[WorkflowExposers.State.fieldName] = (sortOrder, naturalSort) => OrderByElementFactory.Create(DomInstanceExposers.StatusId, sortOrder, naturalSort),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => workflowsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> FilterHandlers => handlers;

		protected override Dictionary<string, Func<SortOrder, bool, IOrderByElement>> OrderByHandlers => orderByHandlers;

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