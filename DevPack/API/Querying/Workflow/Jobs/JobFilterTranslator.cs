namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	internal class JobFilterTranslator : DomInstanceFilterTranslator<Job>
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly FilterElement<DomInstance> jobsDomDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id);
		private readonly Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> handlers;

		public JobFilterTranslator(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

			handlers = new Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>>
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
				[JobExposers.Capabilities.CapabilityId.fieldName] = (comparer, value) => CreateOrchestrationSettingsFilter(SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID, comparer, Convert.ToString(value)),
				[JobExposers.Capabilities.Discretes.fieldName] = (comparer, value) => CreateOrchestrationSettingsFilter(SlcWorkflowIds.Sections.ProfileParameterValues.StringValue, comparer, Convert.ToString(value)),
				[JobExposers.Capacities.CapacityId.fieldName] = (comparer, value) => CreateOrchestrationSettingsFilter(SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID, comparer, Convert.ToString(value)),
				[JobExposers.Configurations.ConfigurationId.fieldName] = (comparer, value) => CreateOrchestrationSettingsFilter(SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID, comparer, Convert.ToString(value)),
				[JobExposers.Properties.PropertyId.fieldName] = (comparer, value) => CreatePropertySettingsFilter(comparer, (Guid)value),
			};
		}

		protected override FilterElement<DomInstance> DomDefinitionFilter => jobsDomDefinitionFilter;

		protected override Dictionary<string, Func<Comparer, object, FilterElement<DomInstance>>> Handlers => handlers;

		/// <summary>
		/// Creates a filter on the orchestration settings of a job. Since the orchestration settings are stored in a
		/// separate DOM instance, the matching configuration instances are resolved first, after which the jobs
		/// referencing those configuration instances are filtered.
		/// </summary>
		private FilterElement<DomInstance> CreateOrchestrationSettingsFilter(FieldDescriptorID field, Comparer comparer, string value)
		{
			var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Configuration.Id)
				.AND(FilterElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(field), comparer, value));

			var configurationIds = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(configurationFilter)
				.Select(x => x.ID.Id)
				.Distinct()
				.ToArray();

			if (configurationIds.Length == 0)
			{
				return new FALSEFilterElement<DomInstance>();
			}

			return new ORFilterElement<DomInstance>(configurationIds
				.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobExecution.JobConfiguration).Equal(x))
				.ToArray());
		}

		/// <summary>
		/// Creates a filter on the property settings of a job. Since the property settings are stored in a separate DOM
		/// instance, the matching property setting collections are resolved first, after which the jobs they are linked
		/// to are filtered.
		/// </summary>
		private FilterElement<DomInstance> CreatePropertySettingsFilter(Comparer comparer, Guid propertyId)
		{
			var collectionFilter = new ANDFilterElement<PropertySettingCollection>(
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope),
				FilterElementFactory.Create(PropertySettingCollectionExposers.PropertySettings.PropertyId, comparer, propertyId));

			var jobIds = planApi.PropertySettingCollections.Read(collectionFilter)
				.Where(x => String.IsNullOrEmpty(x.SubId))
				.Select(x => Guid.TryParse(x.LinkedObjectId, out var linkedObjectId) ? linkedObjectId : Guid.Empty)
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToArray();

			if (jobIds.Length == 0)
			{
				return new FALSEFilterElement<DomInstance>();
			}

			return new ORFilterElement<DomInstance>(jobIds
				.Select(x => DomInstanceExposers.Id.Equal(x))
				.ToArray());
		}

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
