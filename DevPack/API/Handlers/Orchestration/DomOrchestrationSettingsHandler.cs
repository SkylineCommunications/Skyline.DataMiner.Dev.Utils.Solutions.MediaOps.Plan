namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal abstract class DomOrchestrationSettingsHandler<TApiSettings, TDomSetting> : DomInstanceApiObjectValidator<TDomSetting>
		where TApiSettings : OrchestrationSettings
		where TDomSetting : DomInstanceBase
	{
		protected readonly MediaOpsPlanApi planApi;

		protected DomOrchestrationSettingsHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
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
							ConfigurationId = configurationSetting.Id,
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
