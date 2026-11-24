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
	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;
	using WorkflowInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.WorkflowInfo;

	/// <summary>
	/// Maps the <see cref="WorkflowExposers"/> that can be selected in a partial read to the data they are read from, and
	/// converts that data the same way <see cref="Workflow"/> does when it parses a full DOM instance. Fields of the node
	/// sections, which occur multiple times, return a list of values.
	/// </summary>
	internal sealed class WorkflowPartialFieldMapper : IDomPartialFieldMapper<Workflow>
	{
		private WorkflowPartialFieldMapper()
		{
		}

		public static WorkflowPartialFieldMapper Instance { get; } = new WorkflowPartialFieldMapper();

		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[WorkflowExposers.Id.fieldName] = PartialField.Single(x => x.Id.Id),
			[WorkflowExposers.Name.fieldName] = PartialField.ForString(WorkflowInfo.WorkflowName),
			[WorkflowExposers.Description.fieldName] = PartialField.ForString(WorkflowInfo.WorkflowDescription),
			[WorkflowExposers.Notes.fieldName] = PartialField.ForString(WorkflowInfo.WorkflowNotes),
			[WorkflowExposers.JobTypeCategoryId.fieldName] = PartialField.ForString(WorkflowInfo.JobType),
			[WorkflowExposers.Priority.fieldName] = PartialField.Single(ReadPriority, WorkflowInfo.Priority),
			[WorkflowExposers.IsFavorite.fieldName] = PartialField.Single(x => x.TryGetValue<bool>(WorkflowInfo.Favorite, out var value) && value, WorkflowInfo.Favorite),
			[WorkflowExposers.PreRoll.fieldName] = PartialField.Single(x => ReadTimeSpan(x, WorkflowInfo.Preroll), WorkflowInfo.Preroll),
			[WorkflowExposers.PostRoll.fieldName] = PartialField.Single(x => ReadTimeSpan(x, WorkflowInfo.Postroll), WorkflowInfo.Postroll),
			[WorkflowExposers.State.fieldName] = new PartialField(x => ToValues(ReadState(x.PartialObject))) { SelectsStatus = true },

			[WorkflowExposers.Resources.fieldName] = new PartialField(x => x.GetResourceIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeReferences(),
			},
			[WorkflowExposers.ResourcePools.fieldName] = new PartialField(x => x.GetResourcePoolIds().Cast<object>(), Nodes.NodeType, Nodes.NodeReferenceID, Nodes.NodeParentReferenceID)
			{
				CanCombineSections = x => x.CanCombineNodeParentReferences(),
			},
		};

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified workflow fields from the workflow DOM instances.
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
		/// Converts partial workflow DOM instances into partial workflows containing the specified fields.
		/// </summary>
		/// <param name="planApi">The API used to read the full workflow DOM instances whose node sections can't be combined. Only required in that case.</param>
		/// <param name="domPartialObjects">The partial workflow DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Workflow>> CreatePartialWorkflows(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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
			var contexts = domPartialObjects.Select(x => new WorkflowPartialReadContext(x)).ToList();

			if (contexts.Count == 0)
			{
				return Array.Empty<Skyline.DataMiner.SDM.PartialObject<Workflow>>();
			}

			LoadFullInstancesWhereNeeded(planApi, contexts, selected.Select(x => x.Field).ToList());

			return contexts
				.Select(context => new Skyline.DataMiner.SDM.PartialObject<Workflow>(
					context.WorkflowId,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, x.Field.Read(context).ToList()))))
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<Workflow>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Workflow>> IDomPartialFieldMapper<Workflow>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialWorkflows(planApi, domPartialObjects, fields);
		}

		private static void LoadFullInstancesWhereNeeded(MediaOpsPlanApi planApi, IReadOnlyCollection<WorkflowPartialReadContext> contexts, IReadOnlyCollection<PartialField> fields)
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

			var instancesById = RequireApi(planApi).DomHelpers.SlcWorkflowHelper.GetWorkflows(toRead.Select(x => x.WorkflowId).ToList())
				.ToDictionary(x => x.ID.Id);

			foreach (var context in toRead)
			{
				if (instancesById.TryGetValue(context.WorkflowId, out var instance))
				{
					context.FullInstance = instance;
				}
			}
		}

		private static MediaOpsPlanApi RequireApi(MediaOpsPlanApi planApi)
		{
			return planApi ?? throw new InvalidOperationException("The selected fields require reading the full workflow DOM instance.");
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

				if (exposer.FilterType != typeof(Workflow) || !Fields.TryGetValue(exposer.fieldName, out var field))
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

		private static object ReadString(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<string>(descriptor, out var value) ? value : null;
		}

		private static object ReadTimeSpan(DomPartialObject partialObject, FieldDescriptorID descriptor)
		{
			return partialObject.TryGetValue<TimeSpan>(descriptor, out var value) ? value : TimeSpan.Zero;
		}

		private static object ReadPriority(DomPartialObject partialObject)
		{
			return partialObject.TryGetValue<int>(WorkflowInfo.Priority, out var value)
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Priority, WorkflowPriority>((SlcWorkflowIds.Enums.Priority)value)
				: WorkflowPriority.Normal;
		}

		private static object ReadState(DomPartialObject partialObject)
		{
			if (!partialObject.TryGetValue(DomInstanceExposers.StatusId, out var statusId) || String.IsNullOrEmpty(statusId))
			{
				return null;
			}

			return EnumExtensions.MapEnum<SlcWorkflowIds.Behaviors.Workflow_Behavior.StatusesEnum, WorkflowState>(SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.ToEnum(statusId));
		}

		private sealed class PartialField
		{
			public PartialField(Func<WorkflowPartialReadContext, IEnumerable<object>> read, params FieldDescriptorID[] descriptors)
			{
				Read = read;
				Descriptors = descriptors;
			}

			public Func<WorkflowPartialReadContext, IEnumerable<object>> Read { get; }

			public IReadOnlyCollection<FieldDescriptorID> Descriptors { get; }

			public bool SelectsStatus { get; set; }

			/// <summary>
			/// Gets or sets a check whether the values of the sections that occur multiple times can be combined per section.
			/// When it fails for a workflow, the field is read from the full workflow DOM instance.
			/// </summary>
			public Func<WorkflowPartialReadContext, bool> CanCombineSections { get; set; }

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
