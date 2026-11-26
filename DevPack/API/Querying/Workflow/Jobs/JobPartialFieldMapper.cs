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
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;
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
	internal sealed class JobPartialFieldMapper : IDomPartialFieldMapper<Job>
	{
		private JobPartialFieldMapper()
		{
		}

		public static JobPartialFieldMapper Instance { get; } = new JobPartialFieldMapper();

		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[JobExposers.Id.fieldName] = PartialField.Scalar(x => x.JobId),
			[JobExposers.Name.fieldName] = PartialField.ForString(JobInfo.JobName),
			[JobExposers.Key.fieldName] = PartialField.ForString(JobInfo.JobID),
			[JobExposers.Description.fieldName] = PartialField.ForString(JobInfo.JobDescription),
			[JobExposers.Notes.fieldName] = PartialField.ForString(JobInfo.JobNotes),
			[JobExposers.JobTypeCategoryId.fieldName] = PartialField.ForString(JobInfo.JobSource),
			[JobExposers.Start.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.JobStart) ?? default, JobInfo.JobStart),
			[JobExposers.End.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.JobEnd) ?? default, JobInfo.JobEnd),
			[JobExposers.PreRollStart.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.Preroll) ?? ReadDate(x, JobInfo.JobStart) ?? default, JobInfo.Preroll, JobInfo.JobStart),
			[JobExposers.PostRollEnd.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.Postroll) ?? ReadDate(x, JobInfo.JobEnd) ?? default, JobInfo.Postroll, JobInfo.JobEnd),
			[JobExposers.Duration.fieldName] = PartialField.Single(ReadDuration, JobInfo.JobStart, JobInfo.JobEnd),
			[JobExposers.RecurringJobId.fieldName] = PartialField.Single(ReadRecurringJobId, JobInfo.JobSeriesID),
			[JobExposers.ActionRequired.fieldName] = PartialField.Single(x => x.TryGetValue<bool>(JobInfo.ActionNeeded, out var value) && value, JobInfo.ActionNeeded),
			[JobExposers.Priority.fieldName] = PartialField.Single(ReadPriority, JobInfo.JobPriority),
			[JobExposers.State.fieldName] = PartialField.Scalar(x => ReadState(x.PartialObject), selectsStatus: true),
			[JobExposers.ConfigurationState.fieldName] = PartialField.Single(ReadConfigurationState, SlcWorkflowIds.Sections.JobExecution.JobConfigurationStatus),
			[JobExposers.OrganizationId.fieldName] = PartialField.Single(x => ReadGuid(x, SlcWorkflowIds.Sections.CostingAndBilling.Organization), SlcWorkflowIds.Sections.CostingAndBilling.Organization),
			[JobExposers.OwnerId.fieldName] = PartialField.Single(x => ReadGuid(x, SlcWorkflowIds.Sections.CostingAndBilling.JobOwner), SlcWorkflowIds.Sections.CostingAndBilling.JobOwner),

			[JobExposers.HasError.fieldName] = PartialField.Scalar(x => x.GetErrors(includeMessages: false).Count > 0, false, Errors.ErrorCode),
			[JobExposers.Errors.CodeCollection.fieldName] = PartialField.Collection(x => x.GetErrors(includeMessages: false).Select(e => e.Key), Errors.ErrorCode),
			[JobExposers.Errors.MessageCollection.fieldName] = PartialField.Collection(x => x.GetErrors(includeMessages: true).Select(e => e.Value).Where(m => m != null), Errors.ErrorCode, Errors.ErrorMessage)
				.WithCanCombineSections(x => x.CanCombineErrorMessages()),
			[JobExposers.ResourcesCollection.fieldName] = PartialField.Collection(x => x.GetResourceIds(), Nodes.NodeType, Nodes.NodeReferenceID)
				.WithCanCombineSections(x => x.CanCombineNodeReferences()),
			[JobExposers.ResourcePoolsCollection.fieldName] = PartialField.Collection(x => x.GetResourcePoolIds(), Nodes.NodeType, Nodes.NodeReferenceID, Nodes.NodeParentReferenceID)
				.WithCanCombineSections(x => x.CanCombineNodeParentReferences()),
			[JobExposers.Nodes.ConfigurationStateCollection.fieldName] = PartialField.Collection(x => x.GetNodeConfigurationStates(), Nodes.NodeType, Nodes.NodeConfigurationStatus)
				.WithCanCombineSections(x => x.CanCombineNodeConfigurationStates()),

			[JobExposers.Capabilities.CapabilityIdCollection.fieldName] = PartialField.Orchestration(x => x.CapabilityIds),
			[JobExposers.Capabilities.DiscretesCollection.fieldName] = PartialField.Orchestration(x => x.CapabilityDiscretes),
			[JobExposers.Capacities.CapacityIdCollection.fieldName] = PartialField.Orchestration(x => x.CapacityIds),
			[JobExposers.Configurations.ConfigurationIdCollection.fieldName] = PartialField.Orchestration(x => x.ConfigurationIds),

			[JobExposers.Properties.PropertyIdCollection.fieldName] = PartialField.Properties(x => x.PropertyIds),
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
		public static IReadOnlyList<PartialObject<Job>> CreatePartialJobs(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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
				return Array.Empty<PartialObject<Job>>();
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
				.Select(context =>
				{
					var builder = PartialObject<Job>.CreateBuilder();
					foreach (var (exposer, field) in selected)
					{
						field.Include(builder, exposer, context);
					}

					return builder.Build();
				})
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<Job>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<PartialObject<Job>> IDomPartialFieldMapper<Job>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialJobs(planApi, domPartialObjects, fields);
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
			// Partial objects look up values by exposer instance, so the instances themselves are de-duplicated.
			var seen = new HashSet<FieldExposer>();

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(Job) || !Fields.TryGetValue(exposer.fieldName, out var field) || !field.ExposerType.IsInstanceOfType(exposer))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static TimeSpan ReadDuration(DomPartialObject partialObject)
		{
			var start = ReadDate(partialObject, JobInfo.JobStart);
			var end = ReadDate(partialObject, JobInfo.JobEnd);

			return start.HasValue && end.HasValue ? end.Value - start.Value : default;
		}

		private static DateTimeOffset? ReadDate(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<DateTime>(descriptor, out var value) ? (DateTimeOffset?)value : null;
		}

		private static Guid ReadGuid(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<Guid>(descriptor, out var value) ? value : Guid.Empty;
		}

		private static string ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private static Guid ReadRecurringJobId(DomPartialObject partialObject)
		{
			return Guid.TryParse(ReadString(partialObject, JobInfo.JobSeriesID), out var recurringJobId) ? recurringJobId : Guid.Empty;
		}

		private static JobPriority ReadPriority(DomPartialObject partialObject)
		{
			return partialObject.TryGetValue<int>(JobInfo.JobPriority, out var value)
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Jobpriority, JobPriority>((SlcWorkflowIds.Enums.Jobpriority)value)
				: JobPriority.Normal;
		}

		private static JobState ReadState(DomPartialObject partialObject)
		{
			if (!partialObject.TryGetValue(DomInstanceExposers.StatusId, out var statusId) || String.IsNullOrEmpty(statusId))
			{
				return default;
			}

			return EnumExtensions.MapEnum<SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum, JobState>(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToEnum(statusId));
		}

		private static ConfigurationState ReadConfigurationState(DomPartialObject partialObject)
		{
			return partialObject.TryGetValue<int>(SlcWorkflowIds.Sections.JobExecution.JobConfigurationStatus, out var value)
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Jobconfigurationstatus, ConfigurationState>((SlcWorkflowIds.Enums.Jobconfigurationstatus)value)
				: ConfigurationState.Unknown;
		}

		private sealed class PartialField
		{
			private readonly Action<PartialObject<Job>.Builder, FieldExposer, JobPartialReadContext> include;

			private PartialField(Type exposerType, Action<PartialObject<Job>.Builder, FieldExposer, JobPartialReadContext> include, FieldDescriptorID[] descriptors)
			{
				ExposerType = exposerType;
				this.include = include;
				Descriptors = descriptors;
			}

			/// <summary>
			/// Gets the type the selected exposer must have, so the value can be included with the exposer's field type.
			/// </summary>
			public Type ExposerType { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool SelectsStatus { get; private set; }

			public bool RequiresOrchestration { get; private set; }

			public bool RequiresProperties { get; private set; }

			/// <summary>
			/// Gets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a job, the field is read from the full job DOM instance.
			/// </summary>
			public Func<JobPartialReadContext, bool> CanCombineSections { get; private set; }

			public static PartialField Scalar<TField>(Func<JobPartialReadContext, TField> read, bool selectsStatus = false, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(
					typeof(Exposer<Job, TField>),
					(builder, exposer, context) => builder.Include((Exposer<Job, TField>)exposer, read(context)),
					descriptors)
				{
					SelectsStatus = selectsStatus,
				};
			}

			public static PartialField Single<TField>(Func<DomPartialObject, TField> read, params FieldDescriptorID[] descriptors)
			{
				return Scalar(x => read(x.PartialObject), descriptors: descriptors);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor), descriptor);
			}

			public static PartialField Collection<TField>(Func<JobPartialReadContext, IEnumerable<TField>> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(
					typeof(Exposer<Job, IEnumerable<TField>>),
					(builder, exposer, context) => builder.Include((Exposer<Job, IEnumerable<TField>>)exposer, read(context).ToList()),
					descriptors);
			}

			public static PartialField Orchestration<TField>(Func<JobPartialReadContext.OrchestrationValues, IEnumerable<TField>> read)
			{
				var field = Collection(x => read(x.Orchestration), SlcWorkflowIds.Sections.JobExecution.JobConfiguration);
				field.RequiresOrchestration = true;
				return field;
			}

			public static PartialField Properties<TField>(Func<JobPartialReadContext, IEnumerable<TField>> read)
			{
				var field = Collection(read);
				field.RequiresProperties = true;
				return field;
			}

			public PartialField WithCanCombineSections(Func<JobPartialReadContext, bool> canCombineSections)
			{
				CanCombineSections = canCombineSections;
				return this;
			}

			public void Include(PartialObject<Job>.Builder builder, FieldExposer exposer, JobPartialReadContext context)
			{
				include(builder, exposer, context);
			}
		}
	}
}
