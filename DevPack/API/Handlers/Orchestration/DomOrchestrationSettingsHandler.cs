namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal abstract class DomOrchestrationSettingsHandler<TApiSettings, TDomSetting> : DomInstanceApiObjectValidator<TDomSetting>
		where TApiSettings : OrchestrationSettings
		where TDomSetting : DomInstanceBase
	{
		protected readonly MediaOpsPlanApi planApi;

		protected readonly OrchestrationReferenceValidationContext referenceValidationContext;

		protected DomOrchestrationSettingsHandler(MediaOpsPlanApi planApi, OrchestrationReferenceValidationContext referenceValidationContext = null)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.referenceValidationContext = referenceValidationContext;
		}

		protected void CreateOrUpdate(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings == null)
			{
				throw new ArgumentNullException(nameof(apiOrchestrationSettings));
			}

			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			ValidateCapacities(apiOrchestrationSettings);
			ValidateCapabilities(apiOrchestrationSettings);
			ValidateConfigurations(apiOrchestrationSettings);
			ValidateOrchestrationEventReferences(apiOrchestrationSettings);

			var lockResult = planApi.LockManager.LockAndExecute(apiOrchestrationSettings.Where(IsValid).ToList(), CreateOrUpdateDomInstances);
			ReportError(lockResult);
		}

		protected void Delete(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings == null)
			{
				throw new ArgumentNullException(nameof(apiOrchestrationSettings));
			}

			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var lockResult = planApi.LockManager.LockAndExecute(apiOrchestrationSettings.Where(x => !x.IsNew && IsValid(x)).ToList(), DeleteDomInstances);
			ReportError(lockResult);
		}

		protected abstract void CreateOrUpdateDomInstances(ICollection<TApiSettings> apiOrchestrationSettings);

		protected abstract void DeleteDomInstances(ICollection<TApiSettings> apiOrchestrationSettings);

		// Resolves the references contained in the orchestration events of each settings instance and reports the ones
		// that cannot be resolved. The resolver and the reporting behavior are supplied per settings instance by the
		// caller through the optional validation context. When no context is supplied (for example for workflow
		// orchestration settings) no event reference validation is performed.
		private void ValidateOrchestrationEventReferences(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (referenceValidationContext == null)
			{
				return;
			}

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				if (!referenceValidationContext.TryGetTarget(orchestrationSettings.Id, out var resolver, out var owningNodeId, out var reportErrors) || !reportErrors)
				{
					continue;
				}

				foreach (var entry in EnumerateEventReferences(orchestrationSettings))
				{
					var reason = GetUnresolvedReason(resolver, entry, owningNodeId);
					if (reason != null)
					{
						ReportUnresolvedReference(orchestrationSettings, resolver, entry.Reference, reason);
					}
				}

				foreach (var executionDetails in orchestrationSettings.OrchestrationEvents.Select(x => x.ExecutionDetails).Where(x => x != null))
				{
					foreach (var (reference, reason) in GetDynamicInputFailures(resolver, executionDetails, owningNodeId))
					{
						ReportUnresolvedReference(orchestrationSettings, resolver, reference, reason);
					}
				}
			}
		}

		private void ReportUnresolvedReference(TApiSettings orchestrationSettings, ReferenceResolver resolver, DataReference reference, string reason)
		{
			var label = resolver.GetDisplayLabel(reference);
			ReportError(orchestrationSettings.Id, new OrchestrationSettingsUnresolvedReferenceError
			{
				Id = orchestrationSettings.Id,
				Reference = label,
				ErrorMessage = $"Reference '{label}' {reason}",
			});
		}

		// A linked dynamic input is checked against the field it feeds, like a linked profile parameter is.
		private IEnumerable<(DataReference Reference, string Reason)> GetDynamicInputFailures(ReferenceResolver resolver, ScriptExecutionDetails executionDetails, string owningNodeId)
		{
			var linkedInputs = executionDetails.DynamicInputs.Where(x => x.HasReference).ToList();
			if (linkedInputs.Count == 0)
			{
				yield break;
			}

			var inputs = GetDynamicInputDefinition(executionDetails);

			foreach (var input in linkedInputs)
			{
				var resolved = ResolveReference(resolver, input.Reference, owningNodeId);

				// A link to an input the script no longer has is kept for when it returns, so it is only checked for a value.
				var reason = inputs != null && inputs.TryGetField(input.Path, out var field)
					? ResolvedValueConverter.GetFailureReason(resolved, field)
					: ResolvedValueConverter.GetFailureReason(resolved, (Parameter)null);

				if (reason != null)
				{
					yield return (input.Reference, reason);
				}
			}
		}

		private OrchestrationInputDefinition GetDynamicInputDefinition(ScriptExecutionDetails executionDetails)
		{
			try
			{
				// The values of the event decide which inputs exist and what they accept.
				return planApi.LiveApi.Orchestration.Scripts.GetOrchestrationScriptInputInfo(executionDetails.ScriptName, executionDetails.GetDynamicInputValues())?.InputDefinition;
			}
			catch (Exception ex)
			{
				planApi.Logger.Warning(this, $"Failed to evaluate the inputs of orchestration script '{executionDetails.ScriptName}', so its linked inputs are only checked for a value: {ex.Message}");
				return null;
			}
		}

		private static ResolvedValue ResolveReference(ReferenceResolver resolver, DataReference reference, string owningNodeId)
		{
			try
			{
				return resolver.ResolveValue(reference, owningNodeId);
			}
			catch (Exception)
			{
				return null;
			}
		}

		// Returns null when the reference produced a value the parameter it feeds can take. A parameter can be a
		// dropdown, which only holds one of its own options, so the resolved value has to fit it as well.
		private string GetUnresolvedReason(ReferenceResolver resolver, (DataReference Reference, Guid? TargetParameterId) entry, string owningNodeId)
		{
			var resolved = ResolveReference(resolver, entry.Reference, owningNodeId);

			// A script element or parameter has no target parameter, so it takes any value.
			var target = entry.TargetParameterId == null
				? null
				: referenceValidationContext.Definitions.GetParameterDefinition(entry.TargetParameterId.Value);

			return ResolvedValueConverter.GetFailureReason(resolved, target);
		}

		private static IEnumerable<(DataReference Reference, Guid? TargetParameterId)> EnumerateEventReferences(OrchestrationSettings orchestrationSettings)
		{
			return orchestrationSettings.OrchestrationEvents
				.Select(orchestrationEvent => orchestrationEvent.ExecutionDetails)
				.Where(executionDetails => executionDetails != null)
				.SelectMany(EnumerateExecutionDetailReferences);
		}

		private static IEnumerable<(DataReference Reference, Guid? TargetParameterId)> EnumerateExecutionDetailReferences(ScriptExecutionDetails executionDetails)
		{
			return executionDetails.ScriptElements.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)null))
				.Concat(executionDetails.ScriptParameters.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)null)))
				.Concat(executionDetails.Capabilities.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)))
				.Concat(executionDetails.Capacities.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)))
				.Concat(executionDetails.Configurations.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)));
		}

		private void ValidateCapacities(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var capacityIds = apiOrchestrationSettings
				.SelectMany(x => x.Capacities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capacitiesById = planApi.Capacities.Read(capacityIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Capacities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidCapacitySettingsError
					{
						ErrorMessage = $"Capacity with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capacity settings are not allowed.",
						CapacityId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capacitySetting in orchestrationSettings.Capacities)
				{
					if (capacitySetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidCapacitySettingsError
						{
							ErrorMessage = "Capacity ID cannot be empty.",
							CapacityId = capacitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					if (!capacitiesById.TryGetValue(capacitySetting.Id, out var capacity))
					{
						var error = new OrchestrationSettingsInvalidCapacitySettingsError
						{
							ErrorMessage = $"Capacity with ID '{capacitySetting.Id}' not found.",
							CapacityId = capacitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
					}

					PassTraceData(OrchestrationSettingsCapacitySettingValidator.Validate(orchestrationSettings.Id, capacity, capacitySetting, capacitySetting.HasValue));
				}
			}
		}

		private void ValidateCapabilities(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var capabilityIds = apiOrchestrationSettings
				.SelectMany(x => x.Capabilities)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var capabilitiesById = planApi.Capabilities.Read(capabilityIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Capabilities
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidCapabilitySettingsError
					{
						ErrorMessage = $"Capability with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capability settings are not allowed.",
						CapabilityId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var capabilitySetting in orchestrationSettings.Capabilities)
				{
					if (capabilitySetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = "Capability ID cannot be empty.",
							CapabilityId = capabilitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					if (!capabilitiesById.TryGetValue(capabilitySetting.Id, out var capability))
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = $"Capability with ID '{capabilitySetting.Id}' not found.",
							CapabilityId = capabilitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					if (!capabilitySetting.HasValue)
					{
						continue;
					}

					if (!capability.Discretes.Contains(capabilitySetting.Value))
					{
						var error = new OrchestrationSettingsInvalidCapabilitySettingsError
						{
							ErrorMessage = $"Discrete value '{capabilitySetting.Value}' is not valid for capability '{capability.Name}'.",
							CapabilityId = capabilitySetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
					}
				}
			}
		}

		private void ValidateConfigurations(ICollection<TApiSettings> apiOrchestrationSettings)
		{
			if (apiOrchestrationSettings.Count == 0)
			{
				return;
			}

			var configurationIds = apiOrchestrationSettings
				.SelectMany(x => x.Configurations)
				.Select(x => x.Id)
				.Distinct()
				.ToList();

			var configurationsById = planApi.Configurations.Read(configurationIds).ToDictionary(x => x.Id);

			foreach (var orchestrationSettings in apiOrchestrationSettings)
			{
				var duplicateSettings = orchestrationSettings.Configurations
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicateSettings)
				{
					var error = new OrchestrationSettingsInvalidConfigurationSettingsError
					{
						ErrorMessage = $"Configuration with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate configuration settings are not allowed.",
						ConfigurationId = kvp.Key,
						Id = orchestrationSettings.Id,
					};

					ReportError(orchestrationSettings.Id, error);
				}

				if (duplicateSettings.Count > 0)
				{
					continue;
				}

				foreach (var configurationSetting in orchestrationSettings.Configurations)
				{
					if (configurationSetting.Id == Guid.Empty)
					{
						var error = new OrchestrationSettingsInvalidConfigurationSettingsError
						{
							ErrorMessage = "Configuration ID cannot be empty.",
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					if (!configurationsById.TryGetValue(configurationSetting.Id, out var configuration))
					{
						var error = new OrchestrationSettingsInvalidConfigurationSettingsError
						{
							ErrorMessage = $"Configuration with ID '{configurationSetting.Id}' not found.",
							ConfigurationId = configurationSetting.Id,
							Id = orchestrationSettings.Id,
						};

						ReportError(orchestrationSettings.Id, error);
						continue;
					}

					PassTraceData(OrchestrationSettingsConfigurationSettingValidator.Validate(orchestrationSettings.Id, configuration, configurationSetting, configurationSetting.HasValue));
				}
			}
		}
	}
}
