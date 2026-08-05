namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal class RecurringJobFilterTranslator : DomInstanceFilterTranslator<RecurringJob>
	{
		private readonly FilterElement<DomInstance> recurringJobsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
		{
			[RecurringJobExposers.Id.fieldName] = HandleGuid,
			[RecurringJobExposers.Name.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobName), comparer, (string)value),
			[RecurringJobExposers.Description.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobDescription), comparer, (string)value),
			[RecurringJobExposers.Notes.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobNotes), comparer, (string)value),
			[RecurringJobExposers.Priority.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobPriority), comparer, ConvertRecurringJobPriority((RecurringJobPriority)value)),
			[RecurringJobExposers.Start.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobStart), comparer, ((DateTimeOffset)value).UtcDateTime),
			[RecurringJobExposers.Duration.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.RecurringInfo.Duration), comparer, (TimeSpan)value),
			[RecurringJobExposers.DesiredJobState.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.RecurringInfo.DesiredJobStatus), comparer, ConvertDesiredJobState((DesiredJobState)value)),
			[RecurringJobExposers.OrganizationId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.CostingAndBilling.Organization), comparer, (Guid)value),
			[RecurringJobExposers.OwnerId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.CostingAndBilling.JobOwner), comparer, (Guid)value),
			[RecurringJobExposers.JobTypeCategoryId.fieldName] = (comparer, value) => FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.JobSource), comparer, (string)value),
			[RecurringJobExposers.Pattern.EndDate.fieldName] = (comparer, value) => CreatePatternEndDateFilter(comparer, (DateTimeOffset)value),
		};

		protected override FilterElement<DomInstance> DomDefinitionFilter => recurringJobsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		private static int ConvertRecurringJobPriority(RecurringJobPriority priority)
		{
			switch (priority)
			{
				case RecurringJobPriority.High:
					return (int)SlcWorkflowIds.Enums.Jobpriority.High;
				case RecurringJobPriority.Normal:
					return (int)SlcWorkflowIds.Enums.Jobpriority.Normal;
				case RecurringJobPriority.Low:
					return (int)SlcWorkflowIds.Enums.Jobpriority.Low;
				default:
					throw new InvalidOperationException($"Unsupported recurring job priority: {priority}");
			}
		}

		private static int ConvertDesiredJobState(DesiredJobState desiredJobState)
		{
			switch (desiredJobState)
			{
				case DesiredJobState.Draft:
					return (int)SlcWorkflowIds.Enums.Desiredjobstatus.Draft;
				case DesiredJobState.Tentative:
					return (int)SlcWorkflowIds.Enums.Desiredjobstatus.Tentative;
				default:
					throw new InvalidOperationException($"Unsupported desired job state: {desiredJobState}");
			}
		}

		private static FilterElement<DomInstance> CreatePatternEndDateFilter(Comparer comparer, DateTimeOffset endDate)
		{
			// The recurring pattern is stored as a serialized value, so the end date can only be matched textually.
			var serializedEndDate = $"\"EndDate\":{JsonConvert.SerializeObject(endDate)}";

			switch (comparer)
			{
				case Comparer.Equals:
					return DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.RecurringInfo.RecurringPattern).Contains(serializedEndDate);
				case Comparer.NotEquals:
					return DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.RecurringInfo.RecurringPattern).NotContains(serializedEndDate);
				default:
					throw new NotSupportedException($"Comparer {comparer} is not supported for {RecurringJobExposers.Pattern.EndDate.fieldName} checks");
			}
		}
	}
}
