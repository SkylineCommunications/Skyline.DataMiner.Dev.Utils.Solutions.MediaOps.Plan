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
	using JobInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobInfo;
	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;
	using RecurringInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.RecurringInfo;

	/// <summary>
	/// Maps the <see cref="RecurringJobExposers"/> that can be selected in a partial read to the data they are read from,
	/// and converts that data the same way <see cref="RecurringJob"/> does when it parses a full DOM instance. Fields of
	/// sections that occur multiple times (the nodes) return a list of values.
	/// </summary>
	internal sealed class RecurringJobPartialFieldMapper : IDomPartialFieldMapper<RecurringJob>
	{
		private RecurringJobPartialFieldMapper()
		{
		}

		public static RecurringJobPartialFieldMapper Instance { get; } = new RecurringJobPartialFieldMapper();

		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[RecurringJobExposers.Id.fieldName] = PartialField.Single(x => x.Id.Id),
			[RecurringJobExposers.Name.fieldName] = PartialField.ForString(JobInfo.JobName),
			[RecurringJobExposers.Description.fieldName] = PartialField.ForString(JobInfo.JobDescription),
			[RecurringJobExposers.Notes.fieldName] = PartialField.ForString(JobInfo.JobNotes),
			[RecurringJobExposers.JobTypeCategoryId.fieldName] = PartialField.ForString(JobInfo.JobSource),
			[RecurringJobExposers.Start.fieldName] = PartialField.Single(x => ReadDate(x, JobInfo.JobStart), JobInfo.JobStart),
			[RecurringJobExposers.Duration.fieldName] = PartialField.Single(x => x.TryGetValue<TimeSpan>(RecurringInfo.Duration, out var value) ? value : TimeSpan.Zero, RecurringInfo.Duration),
			[RecurringJobExposers.DesiredJobState.fieldName] = PartialField.Single(ReadDesiredJobState, RecurringInfo.DesiredJobStatus),

			// RecurringJob doesn't parse these values from its DOM instance, so a full read always returns their defaults.
			[RecurringJobExposers.Priority.fieldName] = PartialField.Single(_ => RecurringJobPriority.Normal),
			[RecurringJobExposers.OrganizationId.fieldName] = PartialField.Single(_ => Guid.Empty),
			[RecurringJobExposers.OwnerId.fieldName] = PartialField.Single(_ => Guid.Empty),

			[RecurringJobExposers.Pattern.EndDate.fieldName] = PartialField.Single(ReadPatternEndDate, RecurringInfo.RecurringPattern),

			[RecurringJobExposers.Resources.fieldName] = new PartialField(x => x.GetResourceIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeReferences(),
			},
			[RecurringJobExposers.ResourcePools.fieldName] = new PartialField(x => x.GetResourcePoolIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID, Nodes.NodeParentReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeParentReferences(),
			},
		};

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified recurring job fields from the recurring job DOM instances.
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

			foreach (var field in GetFields(fields).Select(x => x.Field))
			{
				foreach (var descriptor in field.Descriptors.Where(x => descriptorIds.Add(x.Id)))
				{
					selectedFields.Add(descriptor);
				}
			}

			if (descriptorIds.Count == 0)
			{
				// Selected when the fields aren't read from DOM fields (e.g. only the ID), so the read never selects no fields at all.
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial recurring job DOM instances into partial recurring jobs containing the specified fields.
		/// </summary>
		/// <param name="planApi">The API used to read the full DOM instances when multi-section values can't be combined. Only required in that case.</param>
		/// <param name="domPartialObjects">The partial recurring job DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<RecurringJob>> CreatePartialRecurringJobs(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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
			var contexts = domPartialObjects.Select(x => new RecurringJobPartialReadContext(x)).ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<RecurringJob>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			return contexts
				.Select(context => new Skyline.DataMiner.SDM.PartialObject<RecurringJob>(
					context.RecurringJobId,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, x.Field.Read(context).ToList()))))
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<RecurringJob>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<RecurringJob>> IDomPartialFieldMapper<RecurringJob>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialRecurringJobs(planApi, domPartialObjects, fields);
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<RecurringJobPartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
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

			var api = planApi ?? throw new InvalidOperationException("The selected fields require the full recurring job DOM instance to be read.");
			var instancesById = api.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(toRead.Select(x => x.RecurringJobId).ToList())
				.ToDictionary(x => x.ID.Id);

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.RecurringJobId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
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

				if (exposer.FilterType != typeof(RecurringJob) || !Fields.TryGetValue(exposer.fieldName, out var field))
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

		private static DateTimeOffset? ReadDate(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<DateTime>(descriptor, out var value) ? (DateTimeOffset?)value : null;
		}

		private static object ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private static object ReadDesiredJobState(DomPartialObject partialObject)
		{
			var status = partialObject.TryGetValue<int>(RecurringInfo.DesiredJobStatus, out var value)
				? (SlcWorkflowIds.Enums.Desiredjobstatus)value
				: SlcWorkflowIds.Enums.Desiredjobstatus.Draft;

			return status.MapEnum<SlcWorkflowIds.Enums.Desiredjobstatus, DesiredJobState>();
		}

		private static object ReadPatternEndDate(DomPartialObject partialObject)
		{
			var patternJson = ReadString(partialObject, RecurringInfo.RecurringPattern) as string;
			var pattern = String.IsNullOrEmpty(patternJson) ? new RecurringPattern() : RecurringPattern.Deserialize(patternJson);

			return pattern != null ? pattern.EndDate : default;
		}

		private sealed class PartialField
		{
			public PartialField(Func<RecurringJobPartialReadContext, IEnumerable<object>> read, params FieldDescriptorID[] descriptors)
			{
				Read = read;
				Descriptors = descriptors;
			}

			public Func<RecurringJobPartialReadContext, IEnumerable<object>> Read { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			/// <summary>
			/// Gets or sets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a recurring job, the field is read from the full recurring job DOM instance.
			/// </summary>
			public Func<RecurringJobPartialReadContext, bool> CanCombineSections { get; set; }

			public static PartialField Single(Func<DomPartialObject, object> read, params FieldDescriptorID[] descriptors)
			{
				return new PartialField(x => ToValues(read(x.PartialObject)), descriptors);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Single(x => ReadString(x, descriptor), descriptor);
			}
		}
	}
}
