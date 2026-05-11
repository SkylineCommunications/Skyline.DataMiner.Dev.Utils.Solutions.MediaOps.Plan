namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	/// <summary>
	/// Resolves <see cref="DataReference"/> instances to a display label or a runtime value.
	/// </summary>
	public class LinkResolver
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LinkResolver"/> class.
		/// </summary>
		public LinkResolver()
		{
			Context = ResolveContext.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkResolver"/> class with a resolution context.
		/// </summary>
		/// <param name="context">The resolution context.</param>
		/// <exception cref="ArgumentNullException">Thrown when the context is null.</exception>
		public LinkResolver(ResolveContext context)
		{
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		/// <summary>
		/// Gets the context used for resolving dependencies.
		/// </summary>
		public ResolveContext Context { get; }

		/// <summary>
		/// Builds a human-readable label for the specified reference.
		/// </summary>
		/// <param name="reference">The reference to describe.</param>
		/// 
		/// <returns>The display label.</returns>
		public virtual string GetDisplayLabel(DataReference reference)
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));

			var displayString = reference.Type.GetDescription();

			switch (reference)
			{
				case ResourcePropertyReference rpr:
					{
						var propertyName = GetResourcePropertyName(rpr);
						if (!String.IsNullOrEmpty(propertyName))
							displayString += $": {propertyName}";

						break;
					}

				case CapabilityParameterReference cpr:
					{
						var parameterName = GetCapabilityName(cpr);
						if (!String.IsNullOrEmpty(parameterName))
							displayString += $": {parameterName}";

						break;
					}

				case CapacityParameterReference capr:
					{
						var parameterName = GetCapacityName(capr);
						if (!String.IsNullOrEmpty(parameterName))
							displayString += $": {parameterName}";

						break;
					}

				case ConfigurationParameterReference cfgr:
					{
						var parameterName = GetConfigurationName(cfgr);
						if (!String.IsNullOrEmpty(parameterName))
							displayString += $": {parameterName}";

						break;
					}

				case WorkflowPropertyReference wpr:
					{
						var propertyName = GetWorkflowPropertyName(wpr);
						if (!String.IsNullOrEmpty(propertyName))
							displayString += $": {propertyName}";

						break;
					}

				case JobPropertyReference jpr:
					{
						var propertyName = GetJobPropertyName(jpr);
						if (!String.IsNullOrEmpty(propertyName))
							displayString += $": {propertyName}";

						break;
					}
			}

			if (!String.IsNullOrEmpty(reference.NodeId)
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
		/// 
		/// <returns>A <see cref="ResolvedValue"/> describing the outcome.</returns>
		/// <exception cref="CircularReferenceException">Thrown when a cycle is detected.</exception>
		public virtual ResolvedValue ResolveValue(DataReference reference)
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));

			var visited = new HashSet<DataReference>();

			while (true)
			{
				if (!visited.Add(reference))
				{
					throw new CircularReferenceException(reference);
				}

				var resolved = reference switch
				{
					CapabilityParameterReference cpb => ResolveCapabilityValue(cpb),
					CapacityParameterReference cap => ResolveCapacityValue(cap),
					ConfigurationParameterReference cfg => ResolveConfigurationValue(cfg),
					ResourceNameReference rnr => ResolveResourceName(rnr),
					ResourceLinkedObjectIdReference rlr => ResolveResourceLinkedObjectId(rlr),
					ResourcePropertyReference rpr => ResolveResourcePropertyValue(rpr),
					WorkflowNameReference wnr => ResolveWorkflowName(wnr),
					WorkflowPropertyReference wpr => ResolveWorkflowPropertyValue(wpr),
					JobNameReference jnr => ResolveJobName(jnr),
					JobPropertyReference jpr => ResolveJobPropertyValue(jpr),
					_ => throw new NotSupportedException($"Unsupported reference type: {reference.GetType()}")
				};

				if (resolved == null || resolved.UnresolvedReference == reference)
				{
					return ResolvedValue.FromUnresolvedReference(reference);
				}

				if (resolved.IsResolved)
				{
					return resolved;
				}

				if (resolved.UnresolvedReference == null)
				{
					return ResolvedValue.FromUnresolvedReference(reference);
				}

				reference = resolved.UnresolvedReference;
			}
		}

		/// <summary>
		/// Determines whether the specified reference can be resolved to an actual value.
		/// </summary>
		/// <param name="reference">The reference to check.</param>
		/// <returns><c>true</c> if the reference can be resolved to an actual value; otherwise, <c>false</c>.</returns>
		public virtual bool CanResolve(DataReference reference)
		{
			if (reference == null)
				throw new ArgumentNullException(nameof(reference));

			try
			{
				var resolved = ResolveValue(reference);
				return resolved != null && resolved.IsResolved;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Resolves a capability parameter reference. The reference targets a specific node when
		/// <see cref="DataReference.NodeId"/> is set, otherwise the workflow / job itself.
		/// Default returns an unresolved <see cref="ResolvedValue"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveCapabilityValue(CapabilityParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference.NodeId);

			if (orchestrationSettings != null)
			{
				var capabilitySetting = orchestrationSettings.Capabilities.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (capabilitySetting != null)
				{
					if (capabilitySetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(capabilitySetting.Reference);

					if (capabilitySetting.HasValue)
						return ResolvedValue.FromValue(capabilitySetting.Value);
				}
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a capacity parameter reference. The reference targets a specific node when
		/// <see cref="DataReference.NodeId"/> is set, otherwise the workflow / job itself.
		/// Default returns an unresolved <see cref="ResolvedValue"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveCapacityValue(CapacityParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference.NodeId);

			if (orchestrationSettings != null)
			{
				var capacitySetting = orchestrationSettings.Capacities.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (capacitySetting != null)
				{
					if (capacitySetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(capacitySetting.Reference);

					if (capacitySetting.HasValue)
						return ResolvedValue.FromSettingValue(capacitySetting);
				}
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a configuration parameter reference. The reference targets a specific node when
		/// <see cref="DataReference.NodeId"/> is set, otherwise the workflow / job itself.
		/// Default returns an unresolved <see cref="ResolvedValue"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveConfigurationValue(ConfigurationParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference.NodeId);

			if (orchestrationSettings != null)
			{
				var configurationSetting = orchestrationSettings.Configurations.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (configurationSetting != null)
				{
					if (configurationSetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(configurationSetting.Reference);

					if (configurationSetting.HasValue)
						return ResolvedValue.FromSettingValue(configurationSetting);
				}
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a <see cref="ResourceNameReference"/> using <see cref="GetResource"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveResourceName(ResourceNameReference reference)
		{
			var resource = GetResource(reference);
			return resource == null
				? ResolvedValue.FromUnresolvedReference(reference)
				: ResolvedValue.FromValue(resource.Name);
		}

		/// <summary>
		/// Resolves a <see cref="ResourcePropertyReference"/> using <see cref="GetResource"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveResourcePropertyValue(ResourcePropertyReference reference)
		{
			var resource = GetResource(reference);
			if (resource == null)
				return ResolvedValue.FromUnresolvedReference(reference);

			var propertyValue = resource.Properties.FirstOrDefault(x => x.Id == reference.ResourcePropertyId);
			return propertyValue == null
				? ResolvedValue.FromUnresolvedReference(reference)
				: ResolvedValue.FromValue(propertyValue.Value);
		}

		/// <summary>
		/// Resolves a <see cref="ResourceLinkedObjectIdReference"/> using <see cref="GetResource"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveResourceLinkedObjectId(ResourceLinkedObjectIdReference reference)
		{
			var resource = GetResource(reference);

			return resource switch
			{
				ElementResource elementResource => ResolvedValue.FromValue($"{elementResource.AgentId}/{elementResource.ElementId}"),
				ServiceResource serviceResource => ResolvedValue.FromValue($"{serviceResource.AgentId}/{serviceResource.ServiceId}"),
				VirtualFunctionResource virtualFunctionResource => ResolvedValue.FromValue($"{virtualFunctionResource.AgentId}/{virtualFunctionResource.ElementId}"),
				UnmanagedResource _ => ResolvedValue.FromValue(String.Empty),
				_ => ResolvedValue.FromUnresolvedReference(reference),
			};
		}

		/// <summary>
		/// Resolves a <see cref="WorkflowNameReference"/>. Default implementation reads the name from
		/// <see cref="ResolveContext.Workflow"/> when available, returning the workflow name
		/// or an unresolved <see cref="ResolvedValue"/> when no workflow is set.
		/// </summary>
		protected virtual ResolvedValue ResolveWorkflowName(WorkflowNameReference reference)
		{
			var name = Context?.Workflow?.Name;
			return name != null ? ResolvedValue.FromValue(name) : ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a <see cref="WorkflowPropertyReference"/>.
		/// When <see cref="ResolveContext.WorkflowPropertyValues"/> is populated, those values are
		/// used directly; otherwise an unresolved <see cref="ResolvedValue"/> is returned.
		/// </summary>
		protected virtual ResolvedValue ResolveWorkflowPropertyValue(WorkflowPropertyReference reference)
		{
			if (Context?.WorkflowPropertyValues != null &&
				Context.WorkflowPropertyValues.TryGetValue(reference.WorkflowPropertyId, out var ctxValue))
			{
				return ResolvedValue.FromValue(ctxValue);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a <see cref="JobNameReference"/>. Default implementation reads the name from
		/// <see cref="ResolveContext.Job"/> when available, returning the job name
		/// or an unresolved <see cref="ResolvedValue"/> when no job is set.
		/// </summary>
		protected virtual ResolvedValue ResolveJobName(JobNameReference reference)
		{
			var name = Context?.Job?.Name;
			return name != null ? ResolvedValue.FromValue(name) : ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a <see cref="JobPropertyReference"/>.
		/// When <see cref="ResolveContext.JobPropertyValues"/> is populated, those values are
		/// used directly; otherwise an unresolved <see cref="ResolvedValue"/> is returned.
		/// </summary>
		protected virtual ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			if (Context?.JobPropertyValues != null &&
				Context.JobPropertyValues.TryGetValue(reference.JobPropertyId, out var ctxValue))
			{
				return ResolvedValue.FromValue(ctxValue);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Returns the resource the reference targets when <see cref="DataReference.NodeId"/> is set. References without a node id target the
		/// workflow / job itself, which has no associated resource, so <c>null</c> is returned.
		/// </summary>
		protected virtual Resource GetResource(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return null;
			}

			return Context.ResourcesByNode.TryGetValue(reference.NodeId, out var resource)
				? resource
				: null;
		}

		/// <summary>Returns the display name for a resource property reference.</summary>
		protected virtual string GetResourcePropertyName(ResourcePropertyReference reference)
		{
			if (Context?.ResourceProperties == null)
				return null;

			return Context.ResourceProperties.TryGetValue(reference.ResourcePropertyId, out var property)
				? property?.Name
				: null;
		}

		/// <summary>Returns the display name for a capability parameter reference.</summary>
		protected virtual string GetCapabilityName(CapabilityParameterReference reference)
		{
			if (Context?.CapabilityDefinitions == null)
				return null;

			return Context.CapabilityDefinitions.TryGetValue(reference.ParameterId, out var capability)
				? capability?.Name
				: null;
		}

		/// <summary>Returns the display name for a capacity parameter reference.</summary>
		protected virtual string GetCapacityName(CapacityParameterReference reference)
		{
			if (Context?.CapacityDefinitions == null)
				return null;

			return Context.CapacityDefinitions.TryGetValue(reference.ParameterId, out var capacity)
				? capacity?.Name
				: null;
		}

		/// <summary>Returns the display name for a configuration parameter reference.</summary>
		protected virtual string GetConfigurationName(ConfigurationParameterReference reference)
		{
			if (Context?.ConfigurationDefinitions == null)
				return null;

			return Context.ConfigurationDefinitions.TryGetValue(reference.ParameterId, out var configuration)
				? configuration?.Name
				: null;
		}

		/// <summary>
		/// Returns the display name for a workflow property. Default returns <c>null</c>; the property catalog
		/// is not exposed through <see cref="IMediaOpsPlanApi"/> and must be supplied by derived classes.
		/// </summary>
		protected virtual string GetWorkflowPropertyName(WorkflowPropertyReference reference)
		{
			if (Context?.WorkflowProperties == null)
				return null;

			return Context.WorkflowProperties.TryGetValue(reference.WorkflowPropertyId, out var value)
				? value?.Name
				: null;
		}

		/// <summary>
		/// Returns the display name for a job property. Default returns <c>null</c>; the property catalog
		/// must be supplied by derived classes or through the context.
		/// </summary>
		protected virtual string GetJobPropertyName(JobPropertyReference reference)
		{
			if (Context?.JobProperties == null)
				return null;

			return Context.JobProperties.TryGetValue(reference.JobPropertyId, out var value)
				? value?.Name
				: null;
		}

		/// <summary>
		/// Returns the <see cref="OrchestrationSettings"/> for the given node or the workflow / job itself
		/// when <paramref name="nodeId"/> is <c>null</c> or empty.
		/// </summary>
		/// <param name="nodeId">The node identifier, or <c>null</c> to target the workflow / job level.</param>
		/// <returns>The matching <see cref="OrchestrationSettings"/>, or <c>null</c> when not found.</returns>
		protected virtual OrchestrationSettings GetOrchestrationSettings(string nodeId)
		{
			if (!String.IsNullOrEmpty(nodeId))
			{
				if (Context?.OrchestrationSettingsByNode != null &&
					Context.OrchestrationSettingsByNode.TryGetValue(nodeId, out var nodeSettings))
				{
					return nodeSettings;
				}
			}
			else
			{
				if (Context?.Workflow != null)
				{
					return Context.Workflow.OrchestrationSettings;
				}

				if (Context?.Job != null)
				{
					return Context.Job.OrchestrationSettings;
				}
			}

			return null;
		}

		/// <summary>
		/// Provides display information for a node reference, used to suffix the display label.
		/// Default returns <c>false</c> meaning no suffix is appended.
		/// </summary>
		/// <param name="nodeId">The node identifier from the reference.</param>
		/// <param name="displayName">The human-readable name of the node.</param>
		protected virtual bool TryGetNodeDisplayInfo(string nodeId, out string displayName)
		{
			displayName = null;
			return false;
		}
	}
}
