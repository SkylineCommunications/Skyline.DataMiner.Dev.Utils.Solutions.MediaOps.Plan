namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using PropertyIdSubId = (System.Guid, System.String);

	/// <summary>
	/// Resolves <see cref="DataReference"/> instances to a display label or a runtime value.
	/// </summary>
	public class ReferenceResolver
	{
		private readonly ReferenceDefinitionCache definitions;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolver"/> class.
		/// </summary>
		public ReferenceResolver(IMediaOpsPlanApi planApi)
			: this(planApi, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceResolver"/> class, optionally reusing a definition
		/// cache that is shared by every resolver created during the same operation so the definitions are queried at
		/// most once. The cache is scoped to that operation; when none is supplied a private cache is created.
		/// </summary>
		internal ReferenceResolver(IMediaOpsPlanApi planApi, ReferenceDefinitionCache definitions)
		{
			PlanApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.definitions = definitions ?? new ReferenceDefinitionCache(planApi);
		}

		/// <summary>
		/// Gets the <see cref="IMediaOpsPlanApi"/> instance used to retrieve definitions and property values.
		/// </summary>
		protected IMediaOpsPlanApi PlanApi { get; }

		/// <summary>
		/// Gets the lazily-loaded dictionary of capability definitions keyed by their identifier.
		/// </summary>
		protected IDictionary<Guid, Capability> CapabilityDefinitions => definitions.CapabilityDefinitions;

		/// <summary>
		/// Gets the lazily-loaded dictionary of capacity definitions keyed by their identifier.
		/// </summary>
		protected IDictionary<Guid, Capacity> CapacityDefinitions => definitions.CapacityDefinitions;

		/// <summary>
		/// Gets the lazily-loaded dictionary of configuration definitions keyed by their identifier.
		/// </summary>
		protected IDictionary<Guid, Configuration> ConfigurationDefinitions => definitions.ConfigurationDefinitions;

		/// <summary>
		/// Gets the lazily-loaded dictionary of property definitions keyed by their identifier.
		/// </summary>
		protected IDictionary<Guid, Property> PropertyDefinitions => definitions.PropertyDefinitions;

		/// <summary>
		/// Gets the lazily-loaded dictionary of resource property definitions keyed by their identifier.
		/// </summary>
		protected IDictionary<Guid, ResourceProperty> ResourcePropertyDefinitions => definitions.ResourcePropertyDefinitions;

		/// <summary>
		/// Builds a human-readable label for the specified reference.
		/// </summary>
		/// <param name="reference">The reference to describe.</param>
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

				case JobPropertyReference jpr:
					{
						var propertyName = GetJobPropertyName(jpr);
						if (!String.IsNullOrEmpty(propertyName))
							displayString += $": {propertyName}";

						break;
					}
			}

			if (!String.IsNullOrEmpty(reference.NodeId) &&
				TryGetNodeDisplayInfo(reference.NodeId, out var nodeDisplayName))
			{
				displayString += $" ({nodeDisplayName})";
			}

			return displayString;
		}

		/// <summary>
		/// Resolves the reference to its current value, following chains of references and detecting cycles.
		/// </summary>
		/// <param name="reference">The reference to resolve.</param>
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
		/// Default returns an unresolved <see cref="ResolvedValue"/> when no matching setting is found.
		/// </summary>
		/// <param name="reference">The capability parameter reference to resolve.</param>
		/// <returns>A <see cref="ResolvedValue"/> containing the capability value, or an unresolved value.</returns>
		protected virtual ResolvedValue ResolveCapabilityValue(CapabilityParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference);

			if (orchestrationSettings != null)
			{
				var capabilitySetting = orchestrationSettings.Capabilities.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (capabilitySetting != null)
				{
					if (capabilitySetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(capabilitySetting.Reference);

					if (capabilitySetting.HasValue)
						return ConvertSettingValue(capabilitySetting);
				}
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a capacity parameter reference. The reference targets a specific node when
		/// <see cref="DataReference.NodeId"/> is set, otherwise the workflow / job itself.
		/// Default returns an unresolved <see cref="ResolvedValue"/> when no matching setting is found.
		/// </summary>
		/// <param name="reference">The capacity parameter reference to resolve.</param>
		/// <returns>A <see cref="ResolvedValue"/> containing the capacity value, or an unresolved value.</returns>
		protected virtual ResolvedValue ResolveCapacityValue(CapacityParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference);

			if (orchestrationSettings != null)
			{
				var capacitySetting = orchestrationSettings.Capacities.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (capacitySetting != null)
				{
					if (capacitySetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(capacitySetting.Reference);

					if (capacitySetting.HasValue)
						return ConvertSettingValue(capacitySetting);
				}
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a configuration parameter reference. The reference targets a specific node when
		/// <see cref="DataReference.NodeId"/> is set, otherwise the workflow / job itself.
		/// Default returns an unresolved <see cref="ResolvedValue"/> when no matching setting is found.
		/// </summary>
		/// <param name="reference">The configuration parameter reference to resolve.</param>
		/// <returns>A <see cref="ResolvedValue"/> containing the configuration value, or an unresolved value.</returns>
		protected virtual ResolvedValue ResolveConfigurationValue(ConfigurationParameterReference reference)
		{
			var orchestrationSettings = GetOrchestrationSettings(reference);

			if (orchestrationSettings != null)
			{
				var configurationSetting = orchestrationSettings.Configurations.FirstOrDefault(x => x.Id == reference.ParameterId);

				if (configurationSetting != null)
				{
					if (configurationSetting.HasReference)
						return ResolvedValue.FromUnresolvedReference(configurationSetting.Reference);

					if (configurationSetting.HasValue)
						return ConvertSettingValue(configurationSetting);
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
				: new StringResolvedValue(resource.Name);
		}

		/// <summary>
		/// Resolves a <see cref="ResourcePropertyReference"/> using <see cref="GetResource"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveResourcePropertyValue(ResourcePropertyReference reference)
		{
			var resource = GetResource(reference);
			if (resource == null)
				return ResolvedValue.FromUnresolvedReference(reference);

			var propertySetting = resource.Properties.FirstOrDefault(x => x.Id == reference.ResourcePropertyId);
			return propertySetting == null
				? ResolvedValue.FromUnresolvedReference(reference)
				: new StringResolvedValue(propertySetting.Value);
		}

		/// <summary>
		/// Resolves a <see cref="ResourceLinkedObjectIdReference"/> using <see cref="GetResource"/>.
		/// </summary>
		protected virtual ResolvedValue ResolveResourceLinkedObjectId(ResourceLinkedObjectIdReference reference)
		{
			var resource = GetResource(reference);

			return resource switch
			{
				ElementResource elementResource => new StringResolvedValue($"{elementResource.AgentId}/{elementResource.ElementId}"),
				ServiceResource serviceResource => new StringResolvedValue($"{serviceResource.AgentId}/{serviceResource.ServiceId}"),
				VirtualFunctionResource virtualFunctionResource => new StringResolvedValue($"{virtualFunctionResource.AgentId}/{virtualFunctionResource.ElementId}"),
				UnmanagedResource _ => new StringResolvedValue(String.Empty),
				_ => ResolvedValue.FromUnresolvedReference(reference),
			};
		}

		/// <summary>
		/// Resolves a <see cref="JobNameReference"/>. The base implementation returns an unresolved
		/// <see cref="ResolvedValue"/>. Derived classes override this to return the job name.
		/// </summary>
		/// <param name="reference">The job name reference to resolve.</param>
		/// <returns>A <see cref="ResolvedValue"/> containing the job name, or an unresolved value.</returns>
		protected virtual ResolvedValue ResolveJobName(JobNameReference reference)
		{
			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Resolves a <see cref="JobPropertyReference"/>. The base implementation returns an unresolved
		/// <see cref="ResolvedValue"/>. Derived classes override this to look up the property value.
		/// </summary>
		/// <param name="reference">The job property reference to resolve.</param>
		/// <returns>A <see cref="ResolvedValue"/> containing the property value, or an unresolved value.</returns>
		protected virtual ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <summary>
		/// Returns the display name for a job property. Default returns <c>null</c>; the property catalog
		/// must be supplied by derived classes or through the context.
		/// </summary>
		protected virtual string GetJobPropertyName(JobPropertyReference reference)
		{
			return PropertyDefinitions.TryGetValue(reference.JobPropertyId, out var value)
				? value?.Name
				: null;
		}

		/// <summary>Returns the display name for a resource property reference.</summary>
		protected virtual string GetResourcePropertyName(ResourcePropertyReference reference)
		{
			return ResourcePropertyDefinitions.TryGetValue(reference.ResourcePropertyId, out var property)
				? property?.Name
				: null;
		}

		/// <summary>Returns the display name for a capability parameter reference.</summary>
		protected virtual string GetCapabilityName(CapabilityParameterReference reference)
		{
			return CapabilityDefinitions.TryGetValue(reference.ParameterId, out var capability)
				? capability?.Name
				: null;
		}

		/// <summary>Returns the display name for a capacity parameter reference.</summary>
		protected virtual string GetCapacityName(CapacityParameterReference reference)
		{
			return CapacityDefinitions.TryGetValue(reference.ParameterId, out var capacity)
				? capacity?.Name
				: null;
		}

		/// <summary>Returns the display name for a configuration parameter reference.</summary>
		protected virtual string GetConfigurationName(ConfigurationParameterReference reference)
		{
			return ConfigurationDefinitions.TryGetValue(reference.ParameterId, out var configuration)
				? configuration?.Name
				: null;
		}

		/// <summary>
		/// Returns the resource the reference targets when <see cref="DataReference.NodeId"/> is set. References without a node id target the
		/// workflow / job itself, which has no associated resource, so <c>null</c> is returned.
		/// </summary>
		protected virtual Resource GetResource(DataReference reference)
		{
			return null;
		}

		/// <summary>
		/// Returns the <see cref="OrchestrationSettings"/> for the given reference's node or the workflow / job itself
		/// when <see cref="DataReference.NodeId"/> is <c>null</c> or empty.
		/// </summary>
		/// <param name="reference">The data reference containing the optional node identifier. When <see cref="DataReference.NodeId"/> is set, the node-level settings are targeted; otherwise the workflow / job level.</param>
		/// <returns>The matching <see cref="OrchestrationSettings"/>, or <c>null</c> when not found.</returns>
		protected virtual OrchestrationSettings GetOrchestrationSettings(DataReference reference)
		{
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

		/// <summary>
		/// Converts a <see cref="PropertySettingBase"/> to a <see cref="ResolvedValue"/>.
		/// </summary>
		/// <param name="propertySetting">The property setting to convert.</param>
		/// <returns>The corresponding <see cref="ResolvedValue"/>.</returns>
		protected ResolvedValue ConvertPropertySetting(PropertySettingBase propertySetting)
		{
			return propertySetting switch
			{
				StringPropertySetting stringPv => new StringResolvedValue(stringPv.Value),
				BooleanPropertySetting boolPv => new BooleanResolvedValue(boolPv.Value),
				DiscretePropertySetting discretePv => new StringResolvedValue(discretePv.Value),
				CustomPropertySetting customPv => new StringResolvedValue(customPv.Value),
				_ => null,
			};
		}

		/// <summary>
		/// Converts a <see cref="Setting"/> to a <see cref="ResolvedValue"/> based on its type and value. Returns <c>null</c> if the setting type is not recognized.
		/// </summary>
		/// <param name="setting">The setting to convert.</param>
		/// <returns>The corresponding <see cref="ResolvedValue"/>.</returns>
		protected ResolvedValue ConvertSettingValue(Setting setting)
		{
			return setting switch
			{
				CapabilitySetting cs => cs.Value != null ? new StringResolvedValue(cs.Value) : new NullResolvedValue(),
				NumberCapacitySetting ncs => ncs.Value != null ? new DecimalResolvedValue(ncs.Value.Value) : new NullResolvedValue(),
				RangeCapacitySetting rcs => rcs.MaxValue != null ? new DecimalResolvedValue(rcs.MaxValue.Value) : new NullResolvedValue(),
				TextConfigurationSetting tcs => tcs.Value != null ? new StringResolvedValue(tcs.Value) : new NullResolvedValue(),
				NumberConfigurationSetting nfcs => nfcs.Value != null ? new DecimalResolvedValue(nfcs.Value.Value) : new NullResolvedValue(),
				DiscreteTextConfigurationSetting dtcs => dtcs.Value?.Value != null ? new StringResolvedValue(dtcs.Value.Value) : new NullResolvedValue(),
				DiscreteNumberConfigurationSetting dncs => dncs.Value?.Value != null ? new DecimalResolvedValue(dncs.Value.Value) : new NullResolvedValue(),
				_ => null,
			};
		}

		/// <summary>
		/// Reads all property settings linked to the specified object identifier and returns them in a dictionary keyed by property id and sub id.
		/// </summary>
		/// <param name="linkedObjectId">The identifier of the linked object.</param>
		/// <returns>A dictionary containing the property settings keyed by property id and sub id.</returns>
		protected IDictionary<PropertyIdSubId, PropertySettingBase> ReadPropertySettings(Guid linkedObjectId)
		{
			var result = new Dictionary<PropertyIdSubId, PropertySettingBase>();

			if (linkedObjectId == Guid.Empty)
			{
				return result;
			}

			var filter = PropertySettingCollectionExposers.LinkedObjectId.Equal(Convert.ToString(linkedObjectId))
				.AND(PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope));
			var collections = PlanApi.PropertySettingCollections.Read(filter);

			foreach (var collection in collections)
			{
				foreach (var propertySetting in collection.OfType<PropertySetting>())
				{
					result[(propertySetting.Id, collection.SubId)] = propertySetting;
				} 
			}

			return result;
		}
	}
}
