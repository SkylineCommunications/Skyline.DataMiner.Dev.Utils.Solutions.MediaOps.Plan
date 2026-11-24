namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Errors = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Errors;
	using JobInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo;
	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;

	/// <summary>
	/// Maps the <see cref="JobExposers"/> that can be selected in a partial read to the data they are read from, and
	/// converts that data the same way <see cref="Job"/> does when it parses a full DOM instance. Fields of sections
	/// that occur multiple times, of the orchestration settings and of the property settings return a list of values.
	/// </summary>
	internal static class JobPartialFieldMapper
	{
		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[JobExposers.Id.fieldName] = PartialField.Single(x => x.Id.Id),
			[JobExposers.Name.fieldName] = PartialField.ForString(JobInfo.JobName),
			[JobExposers.Key.fieldName] = PartialField.ForString(JobInfo.JobID),
			[JobExposers.Description.fieldName] = PartialField.ForString(JobInfo.JobDescription),
			[JobExposers.Notes.fieldName] = PartialField.ForString(JobInfo.JobNotes),
			[JobExposers.JobTypeCategoryId.fieldName] = PartialField.ForString(JobInfo.JobSource),
			[JobExposers.Start.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.JobStart), JobInfo.JobStart),
			[JobExposers.End.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.JobEnd), JobInfo.JobEnd),
			[JobExposers.PreRollStart.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.Preroll) ?? ReadDate(x, JobInfo.JobStart), JobInfo.Preroll, JobInfo.JobStart),
			[JobExposers.PostRollEnd.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.Postroll) ?? ReadDate(x, JobInfo.JobEnd), JobInfo.Postroll, JobInfo.JobEnd),
			[JobExposers.Duration.fieldName] = PartialField.Single(ReadDuration, JobInfo.JobStart, JobInfo.JobEnd),
			[JobExposers.RecurringJobId.fieldName] = PartialField.Single(ReadRecurringJobId, JobInfo.JobSeriesID),
			[JobExposers.ActionRequired.fieldName] = PartialField.Single(x => x.TryGetValue<bool>(JobInfo.ActionNeeded, out var value) && value, JobInfo.ActionNeeded),
			[JobExposers.Priority.fieldName] = PartialField.Single(ReadPriority, JobInfo.JobPriority),
			[JobExposers.State.fieldName] = new PartialField(x => ToValues(ReadState(x.PartialObject))) { SelectsStatus = true },
			[JobExposers.ConfigurationState.fieldName] = PartialField.Single(ReadConfigurationState, SlcWorkflowIds.Sections.JobExecution.JobConfigurationStatus),
			[JobExposers.OrganizationId.fieldName] = PartialField.Single(x => ReadGuid(x, SlcWorkflowIds.Sections.CostingAndBilling.Organization), SlcWorkflowIds.Sections.CostingAndBilling.Organization),
			[JobExposers.OwnerId.fieldName] = PartialField.Single(x => ReadGuid(x, SlcWorkflowIds.Sections.CostingAndBilling.JobOwner), SlcWorkflowIds.Sections.CostingAndBilling.JobOwner),

			[JobExposers.HasError.fieldName] = new PartialField(x => ToValues(x.GetErrors(includeMessages: false).Count > 0), Errors.ErrorCode),
			[JobExposers.Errors.Code.fieldName] = new PartialField(x => x.GetErrors(includeMessages: false).Select(e => (object)e.Key), Errors.ErrorCode),
			[JobExposers.Errors.Message.fieldName] = new PartialField(x => x.GetErrors(includeMessages: true).Select(e => e.Value).Where(m => m != null), Errors.ErrorCode, Errors.ErrorMessage)
			{
				CanCombineSections = x => x.CanCombineErrorMessages(),
			},
			[JobExposers.Resources.fieldName] = new PartialField(x => x.GetResourceIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeReferences(),
			},
			[JobExposers.ResourcePools.fieldName] = new PartialField(x => x.GetResourcePoolIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID, Nodes.NodeParentReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeParentReferences(),
			},
			[JobExposers.Nodes.ConfigurationState.fieldName] = new PartialField(x => x.GetNodeConfigurationStates().Cast<object>(), Nodes.NodeType, Nodes.NodeConfigurationStatus)
			{
				CanCombineSections = x => x.CanCombineNodeConfigurationStates(),
			},

			[JobExposers.Capabilities.CapabilityId.fieldName] = PartialField.Orchestration(x => x.CapabilityIds.Cast<object>()),
			[JobExposers.Capabilities.Discretes.fieldName] = PartialField.Orchestration(x => x.CapabilityDiscretes),
			[JobExposers.Capacities.CapacityId.fieldName] = PartialField.Orchestration(x => x.CapacityIds.Cast<object>()),
			[JobExposers.Configurations.ConfigurationId.fieldName] = PartialField.Orchestration(x => x.ConfigurationIds.Cast<object>()),

			[JobExposers.Properties.PropertyId.fieldName] = new PartialField(x => x.PropertyIds.Cast<object>()) { RequiresProperties = true },
		};

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified job fields from the job DOM instances.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static SelectedFields<DomInstance> CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selectedFields = new SelectedFields<DomInstance>();
			var descriptorIds = new HashSet<Guid>();
			var selectsStatus = false;

			foreach (var field in GetFields(fields).Select(x => x.Field))
			{
				selectsStatus |= field.SelectsStatus;

				foreach (var descriptor in field.Descriptors.Where(x => descriptorIds.Add(x.Id)))
				{
					selectedFields.Add(descriptor);
				}
			}

			if (selectsStatus || descriptorIds.Count == 0)
			{
				// Also selected when the fields are read elsewhere (e.g. only the ID), so the read never selects no fields at all.
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial job DOM instances into partial jobs containing the specified fields. Data stored outside the
		/// job DOM instances is read once for all of them.
		/// </summary>
		/// <param name="planApi">The API used to read data stored outside the job DOM instances. Only required when such fields are selected.</param>
		/// <param name="domPartialObjects">The partial job DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Job>> CreatePartialJobs(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			if (domPartialObjects == null)
			{
				throw new ArgumentNullException(nameof(domPartialObjects));
			}

			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selected = GetFields(fields).ToList();
			var contexts = domPartialObjects.Select(x => new JobPartialReadContext(x)).ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<Job>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			if (selected.Any(x => x.Field.RequiresOrchestration))
			{
				JobPartialReadContext.LoadOrchestration(RequireApi(planApi), contexts);
			}

			if (selected.Any(x => x.Field.RequiresProperties))
			{
				JobPartialReadContext.LoadProperties(RequireApi(planApi), contexts);
			}

			return contexts
				.Select(context => new Skyline.DataMiner.SDM.PartialObject<Job>(
					context.JobId,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, x.Field.Read(context).ToList()))))
				.ToList();
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<JobPartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
		{
			var checks = fields.Where(x => x.CanCombineSections != null).Select(x => x.CanCombineSections).ToList();
			if (checks.Count == 0)
			{
				return;
			}

			var toRead = contexts.Where(context => checks.Any(canCombine => !canCombine(context))).ToList();
			if (toRead.Count == 0)
			{
				return;
			}

			var instancesById = RequireApi(planApi).DomHelpers.SlcWorkflowHelper.GetJobs(toRead.Select(x => x.JobId).ToList())
				.ToDictionary(x => x.ID.Id);

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.JobId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
		}

		private static MediaOpsPlanApi RequireApi(MediaOpsPlanApi planApi)
		{
			return planApi ?? throw new InvalidOperationException("The selected fields require data that is stored outside the job DOM instance.");
		}

		private static IEnumerable<(FieldExposer Exposer, PartialField Field)> GetFields(IEnumerable<FieldExposer> fields)
		{
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(Job) || !Fields.TryGetValue(exposer.fieldName, out var field))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static IEnumerable<object> ToValues(object value)
		{
			return value == null ? Enumerable.Empty<object>() : new[] { value };
		}

		private static object ReadDuration(DomPartialObject partialObject)
		{
			var start = ReadDate(partialObject, JobInfo.JobStart);
			var end = ReadDate(partialObject, JobInfo.JobEnd);

			return start.HasValue && end.HasValue ? (object)(end.Value - start.Value) : null;
		}

		private static DateTimeOffset? ReadDate(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<DateTime>(descriptor, out var value) ? (DateTimeOffset?)value : null;
		}

		private static Guid ReadGuid(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<Guid>(descriptor, out var value) ? value : Guid.Empty;
		}

		private static object ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private static object ReadRecurringJobId(DomPartialObject partialObject)
		{
			return Guid.TryParse(ReadString(partialObject, JobInfo.JobSeriesID) as string, out var recurringJobId) ? recurringJobId : Guid.Empty;
		}

		private static object ReadPriority(DomPartialObject partialObject)
		{
			return partialObject.TryGetValue<int>(JobInfo.JobPriority, out var value)
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Jobpriority, JobPriority>((SlcWorkflowIds.Enums.Jobpriority)value)
				: JobPriority.Normal;
		}

		private static object ReadState(DomPartialObject partialObject)
		{
			if (!partialObject.TryGetValue(DomInstanceExposers.StatusId, out var statusId) || String.IsNullOrEmpty(statusId))
			{
				return null;
			}

			return EnumExtensions.MapEnum<SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum, JobState>(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToEnum(statusId));
		}

		private static object ReadConfigurationState(DomPartialObject partialObject)
		{
			return partialObject.TryGetValue<int>(SlcWorkflowIds.Sections.JobExecution.JobConfigurationStatus, out var value)
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Jobconfigurationstatus, ConfigurationState>((SlcWorkflowIds.Enums.Jobconfigurationstatus)value)
				: ConfigurationState.Unknown;
		}

		private sealed class PartialField
		{
			public PartialField(Func<JobPartialReadContext, IEnumerable<object>> read, params FieldDescriptorID[] descriptors)
			{
				Read = read;
				Descriptors = descriptors;
			}

			public Func<JobPartialReadContext, IEnumerable<object>> Read { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool SelectsStatus { get; set; }

			public bool RequiresOrchestration { get; set; }

			public bool RequiresProperties { get; set; }

			/// <summary>
			/// Gets or sets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a job, the field is read from the full job DOM instance.
			/// </summary>
			public Func<JobPartialReadContext, bool> CanCombineSections { get; set; }

			public static PartialField Single(Func<DomPartialObject, object> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(x => ToValues(read(x.PartialObject)), descriptors);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor), descriptor);
			}

			public static PartialField Orchestration(Func<JobPartialReadContext.OrchestrationValues, IEnumerable<object>> read)
			{
				return new PartialField(x => read(x.Orchestration), SlcWorkflowIds.Sections.JobExecution.JobConfiguration) { RequiresOrchestration = true };
			}
		}
	}
}
