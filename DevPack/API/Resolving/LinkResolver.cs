namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	/// <summary>
	/// Resolves <see cref="DataReference"/> instances to a display label or a runtime value,
	/// using only the <see cref="IMediaOpsPlanApi"/> as its data source.
	/// </summary>
	/// <remarks>
	/// Scripts that need to expose unsaved (in-memory) state to the resolution process can
	/// derive from this class and override the protected virtual hooks. The base implementation
	/// resolves what it can from <see cref="IMediaOpsPlanApi"/> and otherwise returns
	/// <see cref="ResolvedValue.FromUnresolvedReference"/>.
	/// </remarks>
	public abstract class LinkResolver
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkResolver"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API used as the default data source.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planApi"/> is <c>null</c>.</exception>

        protected LinkResolver(IMediaOpsPlanApi planApi)
        {
            PlanApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        /// <summary>Gets the MediaOps Plan API used as the default data source.</summary>
        protected IMediaOpsPlanApi PlanApi { get; }

        /// <summary>
        /// Builds a human-readable label for the specified reference.
        /// </summary>
        /// <param name="reference">The reference to describe.</param>
        /// <param name="context">Resolution context.</param>
        /// <returns>The display label.</returns>
        public string GetDisplayLabel(DataReference reference, ResolveContext context)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var displayString = reference.Type.GetDescription();

            switch (reference)
            {
                case ResourcePropertyReference rpr:
                    if (TryFindResourceProperty(rpr.ResourcePropertyId, out var resourceProperty))
                    {
                        displayString += $": {resourceProperty.Name}";
                    }

                    break;

                case CapabilityParameterReference cpr:
                    {
                        var parameterName = TryGetCapabilityDisplayName(cpr, context);
                        if (!String.IsNullOrEmpty(parameterName))
                            displayString += $": {parameterName}";

                        break;
                    }

                case CapacityParameterReference capr:
                    {
                        var parameterName = TryGetCapacityDisplayName(capr, context);
                        if (!String.IsNullOrEmpty(parameterName))
                            displayString += $": {parameterName}";

                        break;
                    }

                case ConfigurationParameterReference cfgr:
                    {
                        var parameterName = TryGetConfigurationDisplayName(cfgr, context);
                        if (!String.IsNullOrEmpty(parameterName))
                            displayString += $": {parameterName}";

                        break;
                    }

                case WorkflowPropertyReference wpr:
                    {
                        var propertyName = TryGetWorkflowPropertyName(wpr.WorkflowPropertyId);
                        if (!String.IsNullOrEmpty(propertyName))
                        {
                            displayString += $": {propertyName}";
                        }

                        break;
                    }
            }

            if (!String.IsNullOrEmpty(reference.NodeId)
                && !String.Equals(reference.NodeId, context.CurrentNodeId, StringComparison.Ordinal)
                && TryGetNodeDisplayInfo(reference.NodeId, out var nodeDisplayName))
            {
                displayString += $" ({nodeDisplayName})";
            }

            return displayString;
        }

        /// <summary>
        /// Resolves the reference to its current value, following chains of references and detecting cycles.
        /// </summary>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="context">Resolution context.</param>
        /// <returns>A <see cref="ResolvedValue"/> describing the outcome.</returns>
        /// <exception cref="CircularReferenceException">Thrown when a cycle is detected.</exception>
        public ResolvedValue GetValue(DataReference reference, ResolveContext context)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var visited = new HashSet<DataReference>();

            while (true)
            {
                if (!visited.Add(reference))
                {
                    throw new CircularReferenceException(reference);
                }

                var resolved = TryResolve(reference, context);

                if (resolved == null)
                {
                    return ResolvedValue.FromUnresolvedReference(reference);
                }

                if (resolved.IsResolved)
                {
                    return resolved;
                }

                if (resolved.UnresolvedReference == null || resolved.UnresolvedReference == reference)
                {
                    return ResolvedValue.FromUnresolvedReference(reference);
                }

                reference = resolved.UnresolvedReference;
            }
        }

        /// <summary>
        /// Dispatches to the type-specific resolution method.
        /// </summary>
        /// <param name="reference">The reference to resolve.</param>
        /// <param name="context">Resolution context.</param>
        /// <returns>The resolved value or <c>null</c> if it cannot be determined.</returns>
        protected virtual ResolvedValue TryResolve(DataReference reference, ResolveContext context)
        {
            switch (reference)
            {
				case CapabilityParameterReference cpb:
					return TryResolveCurrentNodeCapability(cpb, context);

				case CapacityParameterReference cap:
					return TryResolveCurrentNodeCapacity(cap, context);

				case ConfigurationParameterReference cfg:
					return TryResolveCurrentNodeConfiguration(cfg, context);

                case ResourceNameReference rnr:
                    return TryResolveResourceName(rnr, context);

                case ResourceLinkedObjectIdReference rlr:
                    return TryResolveResourceLinkedObjectId(rlr, context);

                case ResourcePropertyReference rpr:
                    return TryResolveResourceProperty(rpr, context);

                case WorkflowNameReference wnr:
                    return TryResolveWorkflowName(wnr, context);

                case WorkflowPropertyReference wpr:
                    return TryResolveWorkflowProperty(wpr);

                default:
                    return null;
            }
        }

        /// <summary>Resolves a capability parameter on the current node. Default returns <c>null</c>.</summary>
        protected virtual ResolvedValue TryResolveCurrentNodeCapability(CapabilityParameterReference reference, ResolveContext context) => null;

        /// <summary>Resolves a capacity parameter on the current node. Default returns <c>null</c>.</summary>
        protected virtual ResolvedValue TryResolveCurrentNodeCapacity(CapacityParameterReference reference, ResolveContext context) => null;

        /// <summary>Resolves a configuration parameter on the current node. Default returns <c>null</c>.</summary>
        protected virtual ResolvedValue TryResolveCurrentNodeConfiguration(ConfigurationParameterReference reference, ResolveContext context) => null;

        /// <summary>Resolves a capability parameter on another node. Default returns <see cref="ResolvedValue.FromUnresolvedReference(DataReference)"/>.</summary>
        protected virtual ResolvedValue TryResolveOtherNodeCapability(CapabilityParameterReference reference, ResolveContext context) => ResolvedValue.FromUnresolvedReference(reference);

        /// <summary>Resolves a capacity parameter on another node. Default returns <see cref="ResolvedValue.FromUnresolvedReference(DataReference)"/>.</summary>
        protected virtual ResolvedValue TryResolveOtherNodeCapacity(CapacityParameterReference reference, ResolveContext context) => ResolvedValue.FromUnresolvedReference(reference);

        /// <summary>Resolves a configuration parameter on another node. Default returns <see cref="ResolvedValue.FromUnresolvedReference(DataReference)"/>.</summary>
        protected virtual ResolvedValue TryResolveOtherNodeConfiguration(ConfigurationParameterReference reference, ResolveContext context) => ResolvedValue.FromUnresolvedReference(reference);

        /// <summary>
        /// Resolves a <see cref="ResourceNameReference"/> using <see cref="GetResourceForReference"/>.
        /// </summary>
        protected virtual ResolvedValue TryResolveResourceName(ResourceNameReference reference, ResolveContext context)
        {
            var resource = GetResourceForReference(reference, context);
            return resource == null
                ? ResolvedValue.FromUnresolvedReference(reference)
                : ResolvedValue.FromValue(resource.Name);
        }

        /// <summary>
        /// Resolves a <see cref="ResourcePropertyReference"/> using <see cref="GetResourceForReference"/>.
        /// </summary>
        protected virtual ResolvedValue TryResolveResourceProperty(ResourcePropertyReference reference, ResolveContext context)
        {
            var resource = GetResourceForReference(reference, context);
            if (resource == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            var propertyValue = resource.Properties.FirstOrDefault(x => x.Id == reference.ResourcePropertyId);
            return propertyValue == null
                ? ResolvedValue.FromUnresolvedReference(reference)
                : ResolvedValue.FromValue(propertyValue.Value);
        }

        /// <summary>
        /// Resolves a <see cref="ResourceLinkedObjectIdReference"/> using <see cref="GetResourceForReference"/>.
        /// </summary>
        protected virtual ResolvedValue TryResolveResourceLinkedObjectId(ResourceLinkedObjectIdReference reference, ResolveContext context)
        {
            var resource = GetResourceForReference(reference, context);
            switch (resource)
            {
                case ElementResource elementResource:
                    return ResolvedValue.FromValue($"{elementResource.AgentId}/{elementResource.ElementId}");
                case ServiceResource serviceResource:
                    return ResolvedValue.FromValue($"{serviceResource.AgentId}/{serviceResource.ServiceId}");
                case VirtualFunctionResource virtualFunctionResource:
                    return ResolvedValue.FromValue($"{virtualFunctionResource.AgentId}/{virtualFunctionResource.ElementId}");
                case UnmanagedResource _:
                    return ResolvedValue.FromValue(String.Empty);
                default:
                    return ResolvedValue.FromUnresolvedReference(reference);
            }
        }

        /// <summary>
        /// Resolves a <see cref="WorkflowNameReference"/>. Default implementation reads the name from
        /// <see cref="IMediaOpsPlanApi.Workflows"/> using <see cref="ResolveContext.WorkflowId"/>
        /// or from <see cref="IMediaOpsPlanApi.Jobs"/> using <see cref="ResolveContext.JobId"/>.
        /// </summary>
        protected virtual ResolvedValue TryResolveWorkflowName(WorkflowNameReference reference, ResolveContext context)
        {
            var value = GetWorkflowName(context);
            return value != null ? ResolvedValue.FromValue(value) : ResolvedValue.FromUnresolvedReference(reference);
        }

        /// <summary>
        /// Resolves a <see cref="WorkflowPropertyReference"/> using <see cref="GetWorkflowPropertyValue"/>.
        /// </summary>
        protected virtual ResolvedValue TryResolveWorkflowProperty(WorkflowPropertyReference reference)
        {
            var value = GetWorkflowPropertyValue(reference.WorkflowPropertyId);
            return value != null ? ResolvedValue.FromValue(value) : ResolvedValue.FromUnresolvedReference(reference);
        }

        /// <summary>
        /// Returns the resource the reference targets — the current-node resource when the reference
        /// has no node id (or matches <see cref="ResolveContext.CurrentNodeId"/>), otherwise
        /// <see cref="GetResourceForNode"/>.
        /// </summary>
        protected virtual Resource GetResourceForReference(DataReference reference, ResolveContext context)
        {
            if (String.IsNullOrEmpty(reference.NodeId)
                || String.Equals(reference.NodeId, context.CurrentNodeId, StringComparison.Ordinal))
            {
                return GetCurrentNodeResource(context);
            }

            return GetResourceForNode(reference.NodeId);
        }

        /// <summary>
        /// Returns the resource assigned to the current node. Default implementation reads it from
        /// <see cref="IMediaOpsPlanApi.Resources"/> when <see cref="ResolveContext.CurrentResourceId"/> is set.
        /// </summary>
        protected virtual Resource GetCurrentNodeResource(ResolveContext context)
        {
            if (context?.CurrentResourceId == null || context.CurrentResourceId == Guid.Empty)
                return null;

            try
            {
                return PlanApi.Resources.Read(context.CurrentResourceId.Value);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the resource assigned to the workflow node identified by <paramref name="nodeId"/>.
        /// Default implementation returns <c>null</c>.
        /// </summary>
        protected virtual Resource GetResourceForNode(string nodeId)
        {
            return null;
        }

        /// <summary>
        /// Looks up a resource property by its identifier. Default implementation reads from
        /// <see cref="IMediaOpsPlanApi.ResourceProperties"/>.
        /// </summary>
        protected virtual bool TryFindResourceProperty(Guid resourcePropertyId, out ResourceProperty property)
        {
            property = PlanApi.ResourceProperties.Read(resourcePropertyId);
            return property != null;
        }

        /// <summary>Returns the display name for a capability parameter reference. Default reads it from <see cref="IMediaOpsPlanApi.Capabilities"/>.</summary>
        protected virtual string TryGetCapabilityDisplayName(CapabilityParameterReference reference, ResolveContext context)
            => TryGetParameterName(reference);

        /// <summary>Returns the display name for a capacity parameter reference. Default reads it from <see cref="IMediaOpsPlanApi.Capacities"/>.</summary>
        protected virtual string TryGetCapacityDisplayName(CapacityParameterReference reference, ResolveContext context)
            => TryGetParameterName(reference);

        /// <summary>Returns the display name for a configuration parameter reference. Default reads it from <see cref="IMediaOpsPlanApi.Configurations"/>.</summary>
        protected virtual string TryGetConfigurationDisplayName(ConfigurationParameterReference reference, ResolveContext context)
            => TryGetParameterName(reference);

        /// <summary>
        /// Returns the display name for a workflow property. Default returns <c>null</c>; the property catalog
        /// is not exposed through <see cref="IMediaOpsPlanApi"/> and must be supplied by derived classes.
        /// </summary>
        protected virtual string TryGetWorkflowPropertyName(Guid workflowPropertyId)
        {
            return null;
        }

        /// <summary>
        /// Returns the workflow / job name for <see cref="DataReferenceType.WorkflowName"/> resolution.
        /// Default implementation reads it from <see cref="IMediaOpsPlanApi.Workflows"/> when
        /// <see cref="ResolveContext.WorkflowId"/> is set, or from <see cref="IMediaOpsPlanApi.Jobs"/> when
        /// <see cref="ResolveContext.JobId"/> is set.
        /// </summary>
        protected virtual string GetWorkflowName(ResolveContext context)
        {
            if (context == null)
                return null;

            try
            {
                if (context.WorkflowId.HasValue && context.WorkflowId.Value != Guid.Empty)
                {
                    return PlanApi.Workflows.Read(context.WorkflowId.Value)?.Name;
                }

                if (context.JobId.HasValue && context.JobId.Value != Guid.Empty)
                {
                    return PlanApi.Jobs.Read(context.JobId.Value)?.Name;
                }
            }
            catch
            {
                // Resolution is best-effort; never throw from a label/value lookup.
            }

            return null;
        }

        /// <summary>
        /// Returns the orchestration settings of the workflow / job in <paramref name="context"/>, or <c>null</c>
        /// when neither <see cref="ResolveContext.WorkflowId"/> nor <see cref="ResolveContext.JobId"/> is supplied.
        /// </summary>
        protected virtual OrchestrationSettings GetWorkflowOrchestrationSettings(ResolveContext context)
        {
            if (context == null)
                return null;

            try
            {
                if (context.WorkflowId.HasValue && context.WorkflowId.Value != Guid.Empty)
                {
                    return PlanApi.Workflows.Read(context.WorkflowId.Value)?.OrchestrationSettings;
                }

                if (context.JobId.HasValue && context.JobId.Value != Guid.Empty)
                {
                    return PlanApi.Jobs.Read(context.JobId.Value)?.OrchestrationSettings;
                }
            }
            catch
            {
                // Resolution is best-effort; never throw from a label/value lookup.
            }

            return null;
        }

        /// <summary>
        /// Returns the value of a workflow property. Default returns <c>null</c>.
        /// </summary>
        protected virtual string GetWorkflowPropertyValue(Guid workflowPropertyId)
        {
            return null;
        }

        /// <summary>
        /// Provides display information for a node reference, used to suffix the display label.
        /// Default returns <c>false</c> meaning no suffix is appended.
        /// </summary>
        /// <param name="nodeId">The node identifier from the reference.</param>
        /// <param name="displayName">The human-readable name of the node.</param>
        /// 
        protected virtual bool TryGetNodeDisplayInfo(string nodeId, out string displayName)
		{
            displayName = null;
            return false;
        }

        /// <summary>Looks up the capability name by <paramref name="reference"/>.ParameterId from <see cref="IMediaOpsPlanApi.Capabilities"/>.</summary>
        protected string TryGetParameterName(CapabilityParameterReference reference)
        {
            if (reference == null || reference.ParameterId == Guid.Empty)
                return null;

            try { return PlanApi.Capabilities.Read(reference.ParameterId)?.Name; }
            catch { return null; }
        }

        /// <summary>Looks up the capacity name by <paramref name="reference"/>.ParameterId from <see cref="IMediaOpsPlanApi.Capacities"/>.</summary>
        protected string TryGetParameterName(CapacityParameterReference reference)
        {
            if (reference == null || reference.ParameterId == Guid.Empty)
                return null;

            try { return PlanApi.Capacities.Read(reference.ParameterId)?.Name; }
            catch { return null; }
        }

        /// <summary>Looks up the configuration name by <paramref name="reference"/>.ParameterId from <see cref="IMediaOpsPlanApi.Configurations"/>.</summary>
        protected string TryGetParameterName(ConfigurationParameterReference reference)
        {
            if (reference == null || reference.ParameterId == Guid.Empty)
                return null;

            try { return PlanApi.Configurations.Read(reference.ParameterId)?.Name; }
            catch { return null; }
        }

        /// <summary>
        /// Returns the parameter name when <paramref name="parameterId"/> is referenced by
        /// <paramref name="settings"/>; otherwise <c>null</c>. Reads only from the repository
        /// matching the settings collection in which the id is found.
        /// </summary>
        protected string TryGetParameterNameFrom(OrchestrationSettings settings, Guid parameterId)
        {
            if (settings == null || parameterId == Guid.Empty)
                return null;

            if (settings.Capabilities.Any(s => s.Id == parameterId))
            {
                try { return PlanApi.Capabilities.Read(parameterId)?.Name; }
                catch { return null; }
            }

            if (settings.Capacities.Any(s => s.Id == parameterId))
            {
                try { return PlanApi.Capacities.Read(parameterId)?.Name; }
                catch { return null; }
            }

            if (settings.Configurations.Any(s => s.Id == parameterId))
            {
                try { return PlanApi.Configurations.Read(parameterId)?.Name; }
                catch { return null; }
            }

            return null;
        }

        /// <summary>Returns the capability name when <paramref name="reference"/> is present in <paramref name="settings"/>; otherwise <c>null</c>.</summary>
        protected string TryGetParameterNameFrom(OrchestrationSettings settings, CapabilityParameterReference reference)
        {
            if (settings == null || reference == null || reference.ParameterId == Guid.Empty)
                return null;

            return settings.Capabilities.Any(s => s.Id == reference.ParameterId)
                ? TryGetParameterName(reference)
                : null;
        }

        /// <summary>Returns the capacity name when <paramref name="reference"/> is present in <paramref name="settings"/>; otherwise <c>null</c>.</summary>
        protected string TryGetParameterNameFrom(OrchestrationSettings settings, CapacityParameterReference reference)
        {
            if (settings == null || reference == null || reference.ParameterId == Guid.Empty)
                return null;

            return settings.Capacities.Any(s => s.Id == reference.ParameterId)
                ? TryGetParameterName(reference)
                : null;
        }

        /// <summary>Returns the configuration name when <paramref name="reference"/> is present in <paramref name="settings"/>; otherwise <c>null</c>.</summary>
        protected string TryGetParameterNameFrom(OrchestrationSettings settings, ConfigurationParameterReference reference)
        {
            if (settings == null || reference == null || reference.ParameterId == Guid.Empty)
                return null;

            return settings.Configurations.Any(s => s.Id == reference.ParameterId)
                ? TryGetParameterName(reference)
                : null;
        }

        /// <summary>
        /// Resolves <paramref name="parameterId"/> against the supplied orchestration settings, returning the persisted
        /// value or chained reference when found. The lookup probes capabilities, capacities and configurations.
        /// </summary>
        protected static ResolvedValue ResolveFromOrchestrationSettings(OrchestrationSettings orchestrationSettings, Guid parameterId, DataReference reference)
        {
            if (orchestrationSettings == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            Setting setting =
                (Setting)orchestrationSettings.Capabilities.FirstOrDefault(s => s.Id == parameterId)
                ?? (Setting)orchestrationSettings.Capacities.FirstOrDefault(s => s.Id == parameterId)
                ?? orchestrationSettings.Configurations.FirstOrDefault(s => s.Id == parameterId);

            return ResolveSetting(setting, reference);
        }

        /// <summary>Resolves a capability <paramref name="reference"/> against the supplied orchestration settings.</summary>
        protected static ResolvedValue ResolveFromOrchestrationSettings(OrchestrationSettings orchestrationSettings, CapabilityParameterReference reference)
        {
            if (reference == null)
                return null;

            if (orchestrationSettings == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            var setting = orchestrationSettings.Capabilities.FirstOrDefault(s => s.Id == reference.ParameterId);
            return ResolveSetting(setting, reference);
        }

        /// <summary>Resolves a capacity <paramref name="reference"/> against the supplied orchestration settings.</summary>
        protected static ResolvedValue ResolveFromOrchestrationSettings(OrchestrationSettings orchestrationSettings, CapacityParameterReference reference)
        {
            if (reference == null)
                return null;

            if (orchestrationSettings == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            var setting = orchestrationSettings.Capacities.FirstOrDefault(s => s.Id == reference.ParameterId);
            return ResolveSetting(setting, reference);
        }

        /// <summary>Resolves a configuration <paramref name="reference"/> against the supplied orchestration settings.</summary>
        protected static ResolvedValue ResolveFromOrchestrationSettings(OrchestrationSettings orchestrationSettings, ConfigurationParameterReference reference)
        {
            if (reference == null)
                return null;

            if (orchestrationSettings == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            var setting = orchestrationSettings.Configurations.FirstOrDefault(s => s.Id == reference.ParameterId);
            return ResolveSetting(setting, reference);
        }

        private static ResolvedValue ResolveSetting(Setting setting, DataReference reference)
        {
            if (setting == null)
                return ResolvedValue.FromUnresolvedReference(reference);

            if (setting.HasReference)
                return ResolvedValue.FromUnresolvedReference(setting.Reference);

            return TryGetSettingValue(setting, out var value)
                ? ResolvedValue.FromValue(value)
                : ResolvedValue.FromUnresolvedReference(reference);
        }

        /// <summary>
        /// Extracts the runtime value of <paramref name="setting"/> based on its concrete type.
        /// </summary>
        protected static bool TryGetSettingValue(Setting setting, out object value)
        {
            switch (setting)
            {
                case CapabilitySetting capabilitySetting:
                    value = capabilitySetting.Value;
                    return capabilitySetting.HasValue;
                case NumberCapacitySetting numberCapacitySetting:
                    value = numberCapacitySetting.Value;
                    return numberCapacitySetting.HasValue;
                case RangeCapacitySetting rangeCapacitySetting:
                    value = rangeCapacitySetting;
                    return rangeCapacitySetting.HasValue;
                case TextConfigurationSetting textConfigurationSetting:
                    value = textConfigurationSetting.Value;
                    return textConfigurationSetting.HasValue;
                case NumberConfigurationSetting numberConfigurationSetting:
                    value = numberConfigurationSetting.Value;
                    return numberConfigurationSetting.HasValue;
                case DiscreteTextConfigurationSetting discreteTextConfigurationSetting:
                    value = discreteTextConfigurationSetting.Value?.Value;
                    return discreteTextConfigurationSetting.HasValue;
                case DiscreteNumberConfigurationSetting discreteNumberConfigurationSetting:
                    value = discreteNumberConfigurationSetting.Value?.Value;
                    return discreteNumberConfigurationSetting.HasValue;
                default:
                    value = null;
                    return false;
            }
        }
    }
}
