namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.ScriptHelper;

	/// <summary>
	/// Calculates the <see cref="ConfigurationState"/> of the orchestration settings of jobs, recurring jobs or their nodes.
	/// </summary>
	/// <remarks>
	/// This calculator determines the configuration state on the fly, based on the orchestration settings of the given objects. All required parameter definitions and orchestration script definitions
	/// are read in batch and cached for the lifetime of the calculator, so a single instance should be reused for a whole batch of jobs (or recurring jobs)
	/// as long as those are not modified.
	/// </remarks>
	internal sealed class ConfigurationStateCalculator
	{
		private readonly IMediaOpsLiveApi _liveApi;

		private readonly List<OrchestrationSettings> _settings;

		private readonly ParameterCache<Capability> _capabilities;
		private readonly ParameterCache<Capacity> _capacities;
		private readonly ParameterCache<Configuration> _configurations;
		private readonly Lazy<bool> _isLiveInstalled;

		private readonly Dictionary<string, ScriptInputRequirements> _requirementsByScriptName = new Dictionary<string, ScriptInputRequirements>();
		private readonly Dictionary<OrchestrationSettings, ConfigurationState> _stateBySettings = new Dictionary<OrchestrationSettings, ConfigurationState>(ReferenceComparer<OrchestrationSettings>.Instance);

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified job.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="job">The job for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, Job job)
			: this(planApi, liveApi, new[] { job ?? throw new ArgumentNullException(nameof(job)) })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified jobs. All parameter definitions
		/// and orchestration script definitions that are referenced by any of the jobs are read in one batch, so a single instance should be used
		/// when jobs are created or updated in bulk.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="jobs">The jobs for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, IEnumerable<Job> jobs)
			: this(planApi, liveApi, GetSettings(jobs, x => x.OrchestrationSettings, x => x.NodeGraph))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified recurring job.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="recurringJob">The recurring job for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, RecurringJob recurringJob)
			: this(planApi, liveApi, new[] { recurringJob ?? throw new ArgumentNullException(nameof(recurringJob)) })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified recurring jobs. All parameter definitions
		/// and orchestration script definitions that are referenced by any of the recurring jobs are read in one batch, so a single instance should be used
		/// when recurring jobs are created or updated in bulk.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="recurringJobs">The recurring jobs for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, IEnumerable<RecurringJob> recurringJobs)
			: this(planApi, liveApi, GetSettings(recurringJobs, x => x.OrchestrationSettings, x => x.NodeGraph))
		{
		}

		private ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, List<OrchestrationSettings> settings)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			_liveApi = liveApi ?? throw new ArgumentNullException(nameof(liveApi));
			_settings = settings;

			_capabilities = new ParameterCache<Capability>(planApi.Capabilities.Read, x => x.Id, () => GetReferencedIds(x => x.Capabilities.Select(y => y.Id)));
			_capacities = new ParameterCache<Capacity>(planApi.Capacities.Read, x => x.Id, () => GetReferencedIds(x => x.Capacities.Select(y => y.Id)));
			_configurations = new ParameterCache<Configuration>(planApi.Configurations.Read, x => x.Id, () => GetReferencedIds(x => x.Configurations.Select(y => y.Id)));
			_isLiveInstalled = new Lazy<bool>(() => _liveApi.IsInstalled());
		}

		/// <summary>
		/// Creates a calculator for the nodes of the specified node graphs. No job level state can be calculated with the returned instance.
		/// </summary>
		/// <typeparam name="TNode">The type of the nodes in the graphs.</typeparam>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="nodeGraphs">The node graphs for which the node configuration states are calculated.</param>
		/// <returns>A calculator for the nodes of the specified node graphs.</returns>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public static ConfigurationStateCalculator ForNodes<TNode>(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, params NodeGraph<TNode>[] nodeGraphs)
			where TNode : NodeBase
		{
			if (nodeGraphs == null)
			{
				throw new ArgumentNullException(nameof(nodeGraphs));
			}

			var settings = new List<OrchestrationSettings>();
			foreach (var nodeGraph in nodeGraphs)
			{
				AddNodeSettings(settings, nodeGraph);
			}

			return new ConfigurationStateCalculator(planApi, liveApi, settings);
		}

		/// <summary>
		/// Gets the configuration state of the specified job itself, based on its job level orchestration settings.
		/// </summary>
		/// <param name="job">The job.</param>
		/// <returns>The configuration state of the job.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="job"/> is <c>null</c>.</exception>
		public ConfigurationState GetJobConfigurationState(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return GetConfigurationState(job.OrchestrationSettings);
		}

		/// <summary>
		/// Gets the configuration state of the specified recurring job itself, based on its job level orchestration settings.
		/// </summary>
		/// <param name="recurringJob">The recurring job.</param>
		/// <returns>The configuration state of the recurring job.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="recurringJob"/> is <c>null</c>.</exception>
		public ConfigurationState GetJobConfigurationState(RecurringJob recurringJob)
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}

			return GetConfigurationState(recurringJob.OrchestrationSettings);
		}

		/// <summary>
		/// Gets the configuration state of the specified node.
		/// </summary>
		/// <param name="node">The node.</param>
		/// <returns>The configuration state of the node.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="node"/> is <c>null</c>.</exception>
		public ConfigurationState GetNodeConfigurationState(NodeBase node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return GetConfigurationState(node.OrchestrationSettings);
		}

		/// <summary>
		/// Determines whether the specified job or one of its nodes is missing mandatory values.
		/// </summary>
		/// <param name="job">The job.</param>
		/// <returns><c>true</c> when the job or one of its nodes has the state <see cref="ConfigurationState.MandatoryValuesMissing"/>; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="job"/> is <c>null</c>.</exception>
		public bool HasMissingMandatoryValues(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return HasMissingMandatoryValues(job.OrchestrationSettings, job.NodeGraph);
		}

		/// <summary>
		/// Determines whether the specified recurring job or one of its nodes is missing mandatory values.
		/// </summary>
		/// <param name="recurringJob">The recurring job.</param>
		/// <returns><c>true</c> when the recurring job or one of its nodes has the state <see cref="ConfigurationState.MandatoryValuesMissing"/>; otherwise, <c>false</c>.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="recurringJob"/> is <c>null</c>.</exception>
		public bool HasMissingMandatoryValues(RecurringJob recurringJob)
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}

			return HasMissingMandatoryValues(recurringJob.OrchestrationSettings, recurringJob.NodeGraph);
		}

		/// <summary>
		/// Calculates the configuration state of the specified orchestration settings.
		/// </summary>
		/// <param name="settings">The orchestration settings.</param>
		/// <returns>The configuration state of the specified orchestration settings, or <see cref="ConfigurationState.Unknown"/> when no settings are available.</returns>
		private ConfigurationState GetConfigurationState(OrchestrationSettings settings)
		{
			if (settings == null)
			{
				return ConfigurationState.Unknown;
			}

			if (_stateBySettings.TryGetValue(settings, out var state))
			{
				return state;
			}

			state = CalculateConfigurationState(settings);
			_stateBySettings[settings] = state;

			return state;
		}

		private static List<OrchestrationSettings> GetSettings<TObject, TNode>(IEnumerable<TObject> objects, Func<TObject, OrchestrationSettings> settingsSelector, Func<TObject, NodeGraph<TNode>> nodeGraphSelector)
			where TObject : class
			where TNode : NodeBase
		{
			if (objects == null)
			{
				throw new ArgumentNullException(nameof(objects));
			}

			var settings = new List<OrchestrationSettings>();

			foreach (var obj in objects)
			{
				if (obj == null)
				{
					continue;
				}

				var objectSettings = settingsSelector(obj);
				if (objectSettings != null)
				{
					settings.Add(objectSettings);
				}

				AddNodeSettings(settings, nodeGraphSelector(obj));
			}

			return settings;
		}

		private static void AddNodeSettings<TNode>(List<OrchestrationSettings> settings, NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				return;
			}

			foreach (var node in nodeGraph.Nodes)
			{
				if (node?.OrchestrationSettings == null)
				{
					continue;
				}

				settings.Add(node.OrchestrationSettings);
			}
		}

		private bool HasMissingMandatoryValues<TNode>(OrchestrationSettings settings, NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			if (GetConfigurationState(settings) == ConfigurationState.MandatoryValuesMissing)
			{
				return true;
			}

			if (nodeGraph == null)
			{
				return false;
			}

			return nodeGraph.Nodes.Any(x => GetConfigurationState(x?.OrchestrationSettings) == ConfigurationState.MandatoryValuesMissing);
		}

		private ConfigurationState CalculateConfigurationState(OrchestrationSettings settings)
		{
			if (settings.OrchestrationEvents.Any(x => !IsEventFullyDefined(x)))
			{
				return ConfigurationState.MandatoryValuesMissing;
			}

			if (MandatoryParametersMissingValues(settings))
			{
				return ConfigurationState.MandatoryValuesMissing;
			}

			if (ParametersMissingValues(settings))
			{
				return ConfigurationState.NonMandatoryValuesMissing;
			}

			if (IsEmpty(settings))
			{
				return ConfigurationState.NoParametersDefined;
			}

			return ConfigurationState.AllValuesProvided;
		}

		private IReadOnlyCollection<Guid> GetReferencedIds(Func<OrchestrationSettings, IEnumerable<Guid>> idSelector)
		{
			var ids = new HashSet<Guid>();

			foreach (var settings in _settings)
			{
				ids.UnionWith(idSelector(settings));
			}

			return ids;
		}

		private bool MandatoryParametersMissingValues(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_capabilities.Get(x.Id), y => y.IsMandatory)))
			{
				return true;
			}

			if (settings.Capacities.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_capacities.Get(x.Id), y => y.IsMandatory)))
			{
				return true;
			}

			if (settings.Configurations.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_configurations.Get(x.Id), y => y.IsMandatory)))
			{
				return true;
			}

			return false;
		}

		private static bool IsMandatory<TParameter>(TParameter parameter, Func<TParameter, bool> isMandatorySelector)
			where TParameter : class
		{
			// A parameter definition that no longer exists cannot be considered mandatory.
			return parameter != null && isMandatorySelector(parameter);
		}

		private bool ParametersMissingValues(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			if (settings.Capacities.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			if (settings.Configurations.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			return false;
		}

		private bool IsEmpty(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any())
			{
				return false;
			}

			if (settings.Capacities.Any())
			{
				return false;
			}

			if (settings.Configurations.Any())
			{
				return false;
			}

			if (settings.OrchestrationEvents.Any(x => !String.IsNullOrEmpty(x.ExecutionDetails.ScriptName)))
			{
				return false;
			}

			return true;
		}

		private bool IsEventFullyDefined(OrchestrationEvent orchestrationEvent)
		{
			if (!_isLiveInstalled.Value)
			{
				return true;
			}

			if (orchestrationEvent?.ExecutionDetails == null || String.IsNullOrWhiteSpace(orchestrationEvent.ExecutionDetails.ScriptName))
			{
				return true;
			}

			var executionDetails = orchestrationEvent.ExecutionDetails;
			var requirements = GetScriptInputRequirements(executionDetails.ScriptName);

			foreach (var element in requirements.Elements)
			{
				var elementSetting = executionDetails.ScriptElements.FirstOrDefault(x => x.Name == element.Name);
				if (elementSetting == null)
				{
					return false;
				}

				var hasElementValue = elementSetting.DmsElementId != default || !String.IsNullOrWhiteSpace(elementSetting.ElementName);
				if (!hasElementValue && !elementSetting.HasReference)
				{
					return false;
				}
			}

			foreach (var parameter in requirements.Parameters)
			{
				if (!IsParameterFullyDefined(parameter, executionDetails))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsParameterFullyDefined(OrchestrationScriptInputParameter parameter, ScriptExecutionDetails executionDetails)
		{
			if (parameter.LinkedProfileParameter != null)
			{
				// Optional profile parameters never block confirmation.
				if (parameter.LinkedProfileParameter.IsOptional == true)
				{
					return true;
				}

				return IsProfileParameterDefined(executionDetails, parameter.LinkedProfileParameter.ID);
			}

			var scriptParameter = executionDetails.ScriptParameters.FirstOrDefault(x => x.Name == parameter.Name);
			return scriptParameter != null && (!String.IsNullOrWhiteSpace(scriptParameter.Value) || scriptParameter.HasReference);
		}

		private static bool IsProfileParameterDefined(ScriptExecutionDetails executionDetails, Guid profileParameterId)
		{
			var capability = executionDetails.Capabilities.FirstOrDefault(x => x.Id == profileParameterId);
			if (capability != null)
			{
				return capability.HasValue || capability.HasReference;
			}

			var capacity = executionDetails.Capacities.FirstOrDefault(x => x.Id == profileParameterId);
			if (capacity != null)
			{
				return capacity.HasValue || capacity.HasReference;
			}

			var configuration = executionDetails.Configurations.FirstOrDefault(x => x.Id == profileParameterId);
			if (configuration != null)
			{
				return configuration.HasValue || configuration.HasReference;
			}

			return false;
		}

		private ScriptInputRequirements GetScriptInputRequirements(string scriptName)
		{
			if (_requirementsByScriptName.TryGetValue(scriptName, out var requirements))
			{
				return requirements;
			}

			var scriptInputInfo = _liveApi.Orchestration.Scripts.GetOrchestrationScriptInputInfo(scriptName);
			if (scriptInputInfo == null)
			{
				requirements = ScriptInputRequirements.Empty;
			}
			else
			{
				requirements = new ScriptInputRequirements(
					scriptInputInfo.Elements.ToList(),
					scriptInputInfo.Parameters.ToList());
			}

			_requirementsByScriptName[scriptName] = requirements;

			return requirements;
		}

		/// <summary>
		/// Caches the parameter definitions that are referenced by the orchestration settings of the calculator. All referenced definitions are
		/// read in one batch on first use. Definitions that are requested but were not part of that batch are read separately and are cached as well.
		/// </summary>
		private sealed class ParameterCache<TParameter>
			where TParameter : class
		{
			private readonly Func<IEnumerable<Guid>, IEnumerable<TParameter>> _reader;
			private readonly Func<TParameter, Guid> _idSelector;
			private readonly Func<IReadOnlyCollection<Guid>> _referencedIdsProvider;
			private readonly Dictionary<Guid, TParameter> _parametersById = new Dictionary<Guid, TParameter>();

			private bool _isInitialized;

			public ParameterCache(Func<IEnumerable<Guid>, IEnumerable<TParameter>> reader, Func<TParameter, Guid> idSelector, Func<IReadOnlyCollection<Guid>> referencedIdsProvider)
			{
				_reader = reader;
				_idSelector = idSelector;
				_referencedIdsProvider = referencedIdsProvider;
			}

			public TParameter Get(Guid id)
			{
				if (!_isInitialized)
				{
					_isInitialized = true;
					Read(_referencedIdsProvider());
				}

				if (!_parametersById.TryGetValue(id, out var parameter))
				{
					Read(new[] { id });
					_parametersById.TryGetValue(id, out parameter);
				}

				return parameter;
			}

			private void Read(IReadOnlyCollection<Guid> ids)
			{
				if (ids.Count == 0)
				{
					return;
				}

				foreach (var parameter in _reader(ids))
				{
					_parametersById[_idSelector(parameter)] = parameter;
				}

				// Cache the parameters that could not be read so they are not requested again.
				foreach (var id in ids.Where(x => !_parametersById.ContainsKey(x)))
				{
					_parametersById[id] = null;
				}
			}
		}

		private sealed class ReferenceComparer<T> : IEqualityComparer<T>
			where T : class
		{
			public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

			private ReferenceComparer()
			{
			}

			public bool Equals(T x, T y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(T obj)
			{
				return RuntimeHelpers.GetHashCode(obj);
			}
		}

		private sealed class ScriptInputRequirements
		{
			public static readonly ScriptInputRequirements Empty = new ScriptInputRequirements(new List<OrchestrationScriptInputElement>(), new List<OrchestrationScriptInputParameter>());

			public ScriptInputRequirements(IReadOnlyCollection<OrchestrationScriptInputElement> elements, IReadOnlyCollection<OrchestrationScriptInputParameter> parameters)
			{
				Elements = elements;
				Parameters = parameters;
			}

			public IReadOnlyCollection<OrchestrationScriptInputElement> Elements { get; }

			public IReadOnlyCollection<OrchestrationScriptInputParameter> Parameters { get; }
		}
	}
}
