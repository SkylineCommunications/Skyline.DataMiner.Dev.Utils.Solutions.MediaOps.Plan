namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using Nodes = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes;
	using Nodetype = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype;

	/// <summary>
	/// Holds what is known about one job during a partial read: the selected fields of the job DOM instance and, when
	/// the selected fields need them, the data stored outside that instance. Values of sections that occur multiple
	/// times are returned by DOM as one list per field, so they are combined per section by position. When the lists
	/// don't line up (e.g. a field that isn't filled in on every section), the full job DOM instance is used instead.
	/// </summary>
	internal sealed class JobPartialReadContext
	{
		public JobPartialReadContext(DomPartialObject partialObject)
		{
			PartialObject = partialObject ?? throw new ArgumentNullException(nameof(partialObject));
		}

		public DomPartialObject PartialObject { get; }

		public Guid JobId => PartialObject.Id.Id;

		/// <summary>
		/// Gets or sets the full job DOM instance, only read when the selected multi-section values can't be combined.
		/// </summary>
		public JobsInstance FullInstance { get; set; }

		public OrchestrationValues Orchestration { get; set; } = OrchestrationValues.Empty;

		public IReadOnlyList<Guid> PropertyIds { get; set; } = Array.Empty<Guid>();

		/// <summary>
		/// Reads the orchestration settings of the jobs in a single batch. The settings are stored in a separate DOM
		/// instance per job, and each value is classified using its profile parameter, the same way
		/// <see cref="WorkflowOrchestrationSettings"/> does.
		/// </summary>
		public static void LoadOrchestration(MediaOpsPlanApi planApi, IReadOnlyCollection<JobPartialReadContext> contexts)
		{
			var configurationIdByJob = new Dictionary<JobPartialReadContext, Guid>();
			foreach (var context in contexts)
			{
				if (context.PartialObject.TryGetValue<Guid>(SlcWorkflowIds.Sections.JobExecution.JobConfiguration, out var configurationId) && configurationId != Guid.Empty)
				{
					configurationIdByJob[context] = configurationId;
				}
			}

			if (configurationIdByJob.Count == 0)
			{
				return;
			}

			var configurationsById = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(configurationIdByJob.Values.Distinct())
				.GroupBy(x => x.ID.Id)
				.ToDictionary(x => x.Key, x => x.First());

			var parameterIds = configurationsById.Values
				.SelectMany(x => x.ProfileParameterValues)
				.Select(x => x.ProfileParameterId)
				.Distinct()
				.ToList();

			var parametersById = planApi.CoreHelpers.ProfileProvider.GetParametersById(parameterIds)
				.GroupBy(x => x.ID)
				.ToDictionary(x => x.Key, x => x.First());

			foreach (var entry in configurationIdByJob)
			{
				if (configurationsById.TryGetValue(entry.Value, out var configuration))
				{
					entry.Key.Orchestration = OrchestrationValues.Create(configuration, parametersById);
				}
			}
		}

		/// <summary>
		/// Reads the property settings of the jobs in a single batch, the same way <see cref="PropertySettingsContext"/>
		/// does for a single job: the settings of the job itself are stored in the collection without a sub ID.
		/// </summary>
		public static void LoadProperties(MediaOpsPlanApi planApi, IReadOnlyCollection<JobPartialReadContext> contexts)
		{
			if (contexts.Count == 0)
			{
				return;
			}

			var filter = new ANDFilterElement<PropertySettingCollection>(
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope),
				new ORFilterElement<PropertySettingCollection>(contexts.Select(x => PropertySettingCollectionExposers.LinkedObjectId.Equal(x.JobId.ToString())).ToArray()));

			var ownerCollections = new Dictionary<string, PropertySettingCollection>(StringComparer.OrdinalIgnoreCase);
			foreach (var collection in planApi.PropertySettingCollections.Read(filter))
			{
				if (collection.LinkedObjectId != null && String.IsNullOrEmpty(collection.SubId))
				{
					ownerCollections[collection.LinkedObjectId] = collection;
				}
			}

			foreach (var context in contexts)
			{
				if (ownerCollections.TryGetValue(context.JobId.ToString(), out var collection))
				{
					context.PropertyIds = collection.PropertySettings.Select(x => x.Id).ToList();
				}
			}
		}

		public bool CanCombineNodeReferences()
		{
			return GetValues<string>(Nodes.NodeReferenceID).Count == GetValues<int>(Nodes.NodeType).Count;
		}

		public bool CanCombineNodeParentReferences()
		{
			var types = GetValues<int>(Nodes.NodeType);
			var parentCount = GetValues<string>(Nodes.NodeParentReferenceID).Count;

			return CanCombineNodeReferences()
				&& (parentCount == types.Count || parentCount == types.Count(x => x == (int)Nodetype.Resource));
		}

		public bool CanCombineNodeConfigurationStates()
		{
			return GetValues<int>(Nodes.NodeConfigurationStatus).Count == GetValues<int>(Nodes.NodeType).Count;
		}

		public bool CanCombineErrorMessages()
		{
			return GetValues<string>(SlcWorkflowIds.Sections.Errors.ErrorMessage).Count == GetValues<string>(SlcWorkflowIds.Sections.Errors.ErrorCode).Count;
		}

		/// <summary>
		/// Gets the resources referenced by the resource nodes, as <see cref="JobExposers.Resources"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetResourceIds()
		{
			return GetNodes(includeReferences: true, includeParentReferences: false, includeConfigurationStates: false)
				.Where(x => x.Type == Nodetype.Resource)
				.Select(x => x.Reference);
		}

		/// <summary>
		/// Gets the resource pools referenced by the nodes, as <see cref="JobExposers.ResourcePools"/> returns them.
		/// </summary>
		public IEnumerable<Guid> GetResourcePoolIds()
		{
			return GetNodes(includeReferences: true, includeParentReferences: true, includeConfigurationStates: false)
				.Select(x => x.Type == Nodetype.Resource ? x.ParentReference : x.Reference)
				.Where(x => x != Guid.Empty);
		}

		/// <summary>
		/// Gets the configuration state of every node, as <see cref="JobExposers.Nodes.ConfigurationState"/> returns them.
		/// </summary>
		public IEnumerable<ConfigurationState> GetNodeConfigurationStates()
		{
			return GetNodes(includeReferences: false, includeParentReferences: false, includeConfigurationStates: true)
				.Select(x => x.ConfigurationState);
		}

		/// <summary>
		/// Gets the errors of the job the same way <see cref="Job"/> parses them: errors without a code are ignored and
		/// a code that occurs multiple times keeps its first position and the message of its last occurrence.
		/// </summary>
		public IReadOnlyCollection<KeyValuePair<string, string>> GetErrors(bool includeMessages)
		{
			var errors = new List<KeyValuePair<string, string>>();
			var indexByCode = new Dictionary<string, int>();

			foreach (var (code, message) in GetErrorSections(includeMessages))
			{
				if (String.IsNullOrWhiteSpace(code))
				{
					continue;
				}

				var error = new KeyValuePair<string, string>(code, message);
				if (indexByCode.TryGetValue(code, out var index))
				{
					errors[index] = error;
				}
				else
				{
					indexByCode[code] = errors.Count;
					errors.Add(error);
				}
			}

			return errors;
		}

		private static Guid ParseReference(string value)
		{
			return !String.IsNullOrEmpty(value) && Guid.TryParse(value, out var id) ? id : Guid.Empty;
		}

		private static ConfigurationState MapNodeConfigurationState(SlcWorkflowIds.Enums.Nodeconfigurationstatus? status)
		{
			return status.HasValue
				? EnumExtensions.MapEnum<SlcWorkflowIds.Enums.Nodeconfigurationstatus, ConfigurationState>(status.Value)
				: ConfigurationState.Unknown;
		}

		private static bool IsSupportedNodeType(Nodetype type)
		{
			// Job ignores nodes of other types when it parses its node graph.
			return type == Nodetype.Resource || type == Nodetype.ResourcePool;
		}

		private List<T> GetValues<T>(FieldDescriptorID descriptor)
		{
			return PartialObject.TryGetValues<T>(descriptor, out var values) && values != null ? values : new List<T>();
		}

		private IEnumerable<NodeValues> GetNodes(bool includeReferences, bool includeParentReferences, bool includeConfigurationStates)
		{
			if (FullInstance != null)
			{
				return FullInstance.Nodes
					.Where(x => x.NodeType.HasValue && IsSupportedNodeType(x.NodeType.Value))
					.Select(x => new NodeValues(x.NodeType.Value, x.ReferenceId, x.ParentReferenceId, MapNodeConfigurationState(x.NodeConfigurationStatus)))
					.ToList();
			}

			var types = GetValues<int>(Nodes.NodeType);
			var references = includeReferences ? GetValues<string>(Nodes.NodeReferenceID) : null;
			var parentReferences = includeParentReferences ? GetValues<string>(Nodes.NodeParentReferenceID) : null;
			var configurationStates = includeConfigurationStates ? GetValues<int>(Nodes.NodeConfigurationStatus) : null;

			// Parent references are only stored on resource nodes, so when not every node has one they belong to the resource nodes in order.
			var parentPerNode = parentReferences != null && parentReferences.Count == types.Count;
			var nextParent = 0;

			var nodes = new List<NodeValues>(types.Count);
			for (int i = 0; i < types.Count; i++)
			{
				var type = (Nodetype)types[i];
				var parentReference = Guid.Empty;

				if (parentReferences != null && type == Nodetype.Resource)
				{
					parentReference = ParseReference(parentPerNode ? parentReferences[i] : parentReferences[nextParent++]);
				}

				if (!IsSupportedNodeType(type))
				{
					continue;
				}

				nodes.Add(new NodeValues(
					type,
					references != null ? ParseReference(references[i]) : Guid.Empty,
					parentReference,
					configurationStates != null ? MapNodeConfigurationState((SlcWorkflowIds.Enums.Nodeconfigurationstatus)configurationStates[i]) : ConfigurationState.Unknown));
			}

			return nodes;
		}

		private IEnumerable<(string Code, string Message)> GetErrorSections(bool includeMessages)
		{
			if (FullInstance != null)
			{
				return FullInstance.Errors.Select(x => (x.ErrorCode, x.ErrorMessage)).ToList();
			}

			var codes = GetValues<string>(SlcWorkflowIds.Sections.Errors.ErrorCode);
			var messages = includeMessages ? GetValues<string>(SlcWorkflowIds.Sections.Errors.ErrorMessage) : null;

			return codes.Select((code, i) => (code, messages != null ? messages[i] : null)).ToList();
		}

		/// <summary>
		/// The orchestration settings values exposed by <see cref="JobExposers"/>, in the order of <see cref="WorkflowOrchestrationSettings"/>.
		/// </summary>
		internal sealed class OrchestrationValues
		{
			public static readonly OrchestrationValues Empty = new OrchestrationValues();

			public List<Guid> CapabilityIds { get; } = new List<Guid>();

			public List<string> CapabilityDiscretes { get; } = new List<string>();

			public List<Guid> CapacityIds { get; } = new List<Guid>();

			public List<Guid> ConfigurationIds { get; } = new List<Guid>();

			public static OrchestrationValues Create(ConfigurationInstance configuration, IReadOnlyDictionary<Guid, Net.Profiles.Parameter> parametersById)
			{
				var values = new OrchestrationValues();
				var numberCapacities = new List<Guid>();
				var rangeCapacities = new List<Guid>();
				var textConfigurations = new List<Guid>();
				var numberConfigurations = new List<Guid>();
				var discreteTextConfigurations = new List<Guid>();
				var discreteNumberConfigurations = new List<Guid>();

				foreach (var section in configuration.ProfileParameterValues)
				{
					if (!parametersById.TryGetValue(section.ProfileParameterId, out var parameter))
					{
						continue;
					}

					if (parameter.IsCapability())
					{
						values.CapabilityIds.Add(section.ProfileParameterId);
						if (section.StringValue != null)
						{
							values.CapabilityDiscretes.Add(section.StringValue);
						}
					}
					else if (parameter.IsCapacity())
					{
						(parameter.IsRange() ? rangeCapacities : numberCapacities).Add(section.ProfileParameterId);
					}
					else if (parameter.IsConfiguration())
					{
						if (parameter.IsText())
						{
							textConfigurations.Add(section.ProfileParameterId);
						}
						else if (parameter.IsNumber())
						{
							numberConfigurations.Add(section.ProfileParameterId);
						}
						else if (parameter.IsTextDiscrete())
						{
							discreteTextConfigurations.Add(section.ProfileParameterId);
						}
						else if (parameter.IsNumberDiscrete())
						{
							discreteNumberConfigurations.Add(section.ProfileParameterId);
						}
					}
				}

				values.CapacityIds.AddRange(numberCapacities.Concat(rangeCapacities));
				values.ConfigurationIds.AddRange(textConfigurations.Concat(numberConfigurations).Concat(discreteTextConfigurations).Concat(discreteNumberConfigurations));

				return values;
			}
		}

		private sealed class NodeValues
		{
			public NodeValues(Nodetype type, Guid reference, Guid parentReference, ConfigurationState configurationState)
			{
				Type = type;
				Reference = reference;
				ParentReference = parentReference;
				ConfigurationState = configurationState;
			}

			public Nodetype Type { get; }

			public Guid Reference { get; }

			public Guid ParentReference { get; }

			public ConfigurationState ConfigurationState { get; }
		}
	}
}
