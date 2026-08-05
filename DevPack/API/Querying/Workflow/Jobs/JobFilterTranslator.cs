namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal class JobFilterTranslator : DomInstanceFilterTranslator<Job>
	{
		private readonly FilterElement<DomInstance> jobsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[JobExposers.Id.fieldName] = HandleGuid,
			[JobExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobName), comparer, (string)value),
			[JobExposers.Key.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobID), comparer, (string)value),
			[JobExposers.Description.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobDescription), comparer, (string)value),
			[JobExposers.Notes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobNotes), comparer, (string)value),
			[JobExposers.Start.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobStart), comparer, ((DateTimeOffset)value).UtcDateTime),
			[JobExposers.End.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobEnd), comparer, ((DateTimeOffset)value).UtcDateTime),
			[JobExposers.PreRollStart.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Preroll), comparer, ((DateTimeOffset)value).UtcDateTime),
			[JobExposers.PostRollEnd.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Postroll), comparer, ((DateTimeOffset)value).UtcDateTime),
			[JobExposers.RecurringJobId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobSeriesID), comparer, Convert.ToString(value)),
			[JobExposers.JobTypeCategoryId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobSource), comparer, (string)value),
			[JobExposers.Priority.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobPriority), comparer, ConvertJobPriority((JobPriority)value)),
			[JobExposers.State.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, ConvertJobState((JobState)value)),
			[JobExposers.OrganizationId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.CostingAndBilling.Organization), comparer, (Guid)value),
			[JobExposers.OwnerId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.CostingAndBilling.JobOwner), comparer, (Guid)value),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => jobsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		private static int ConvertJobPriority(JobPriority priority)
		{
			switch (priority)
			{
				case JobPriority.High:
					return (int)SlcWorkflowIds.Enums.Jobpriority.High;
				case JobPriority.Normal:
					return (int)SlcWorkflowIds.Enums.Jobpriority.Normal;
				case JobPriority.Low:
					return (int)SlcWorkflowIds.Enums.Jobpriority.Low;
				default:
					throw new InvalidOperationException($"Unsupported job priority: {priority}");
			}
		}

		private static string ConvertJobState(JobState state)
		{
			switch (state)
			{
				case JobState.Draft:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Draft;
				case JobState.Tentative:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Tentative;
				case JobState.Confirmed:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Confirmed;
				case JobState.Running:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Running;
				case JobState.Completed:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Completed;
				case JobState.Canceled:
					return SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.Canceled;
				default:
					throw new InvalidOperationException($"Unsupported job state: {state}");
			}
		}
	}
}
