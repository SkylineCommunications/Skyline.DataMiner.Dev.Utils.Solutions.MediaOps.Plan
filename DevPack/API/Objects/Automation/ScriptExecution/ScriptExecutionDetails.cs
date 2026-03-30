namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	/// <summary>
	/// Represents the configuration and settings required to execute a script.
	/// </summary>
	public class ScriptExecutionDetails
	{
		private readonly List<ScriptElementSetting> scriptElementSettings = [];

		private readonly List<ScriptParameterSetting> scriptParameterSettings = [];

		private readonly List<StorageCapabilitySetting> capabilitySettings = [];

		private readonly List<StorageNumberCapacitySetting> numberCapacitySettings = [];

		private readonly List<StorageRangeCapacitySetting> rangeCapacitySettings = [];

		private readonly List<StorageTextConfigurationSetting> textConfigurationSettings = [];

		private readonly List<StorageNumberConfigurationSetting> numberConfigurationSettings = [];

		private readonly List<StorageDiscreteTextConfigurationSetting> discreteTextConfigurationSettings = [];

		private readonly List<StorageDiscreteNumberConfigurationSetting> discreteNumberConfigurationSettings = [];

		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptExecutionDetails"/> class with the specified script name.
		/// </summary>
		/// <param name="scriptName">The name of the script to execute. Cannot be <see langword="null"/> or empty.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptName"/> is <see langword="null"/> or empty.</exception>
		public ScriptExecutionDetails(string scriptName)
		{
			if (string.IsNullOrEmpty(scriptName))
			{
				throw new ArgumentNullException(nameof(scriptName));
			}

			ScriptName = scriptName;
		}

		/// <summary>
		/// Gets the name of the script to execute.
		/// </summary>
		public string ScriptName { get; internal set; }

		/// <summary>
		/// Gets the collection of script element settings.
		/// </summary>
		public IReadOnlyCollection<ScriptElementSetting> ScriptElements => scriptElementSettings;

		/// <summary>
		/// Gets the collection of script parameter settings.
		/// </summary>
		public IReadOnlyCollection<ScriptParameterSetting> ScriptParameters => scriptParameterSettings;

		/// <summary>
		/// Gets the collection of capability settings.
		/// </summary>
		public IReadOnlyCollection<CapabilitySetting> Capabilities => capabilitySettings;

		/// <summary>
		/// Gets the collection of capacity settings.
		/// </summary>
		public IReadOnlyCollection<CapacitySetting> Capacities
		{
			get
			{
				return numberCapacitySettings.Concat<CapacitySetting>(rangeCapacitySettings).ToList();
			}
		}

		/// <summary>
		/// Gets the collection of configuration settings.
		/// </summary>
		public IReadOnlyCollection<ConfigurationSetting> Configurations
		{
			get
			{
				return textConfigurationSettings
					.Concat<ConfigurationSetting>(numberConfigurationSettings)
					.Concat(discreteTextConfigurationSettings)
					.Concat(discreteNumberConfigurationSettings)
					.ToList();
			}
		}

		/// <summary>
		/// Adds a new script element.
		/// </summary>
		/// <param name="scriptElementSetting">The script element to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptElementSetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails AddScriptElement(ScriptElementSetting scriptElementSetting)
		{
			if (scriptElementSetting == null)
			{
				throw new ArgumentNullException(nameof(scriptElementSetting));
			}

			scriptElementSettings.Add(scriptElementSetting);
			return this;
		}

		/// <summary>
		/// Removes the specified script element.
		/// </summary>
		/// <param name="scriptElementSetting">The script element to remove.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptElementSetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails RemoveScriptElement(ScriptElementSetting scriptElementSetting)
		{
			if (scriptElementSetting == null)
			{
				throw new ArgumentNullException(nameof(scriptElementSetting));
			}

			scriptElementSettings.Remove(scriptElementSetting);
			return this;
		}

		/// <summary>
		/// Adds a new script parameter.
		/// </summary>
		/// <param name="scriptParameterSetting">The script parameter to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptParameterSetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails AddScriptParameter(ScriptParameterSetting scriptParameterSetting)
		{
			if (scriptParameterSetting == null)
			{
				throw new ArgumentNullException(nameof(scriptParameterSetting));
			}

			scriptParameterSettings.Add(scriptParameterSetting);
			return this;
		}

		/// <summary>
		/// Removes the specified script parameter.
		/// </summary>
		/// <param name="scriptParameterSetting">The script parameter to remove.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptParameterSetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails RemoveScriptParameter(ScriptParameterSetting scriptParameterSetting)
		{
			if (scriptParameterSetting == null)
			{
				throw new ArgumentNullException(nameof(scriptParameterSetting));
			}

			scriptParameterSettings.Remove(scriptParameterSetting);
			return this;
		}

		/// <summary>
		/// Adds a new capability.
		/// </summary>
		/// <param name="capabilitySetting">The capability setting to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails AddCapability(CapabilitySetting capabilitySetting)
		{
			if (capabilitySetting == null)
			{
				throw new ArgumentNullException(nameof(capabilitySetting));
			}

			capabilitySettings.Add(new StorageCapabilitySetting(capabilitySetting));
			return this;
		}

		/// <summary>
		/// Removes the specified capability.
		/// </summary>
		/// <param name="capabilitySetting">The capability to remove. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails RemoveCapability(CapabilitySetting capabilitySetting)
		{
			if (capabilitySetting == null)
			{
				throw new ArgumentNullException(nameof(capabilitySetting));
			}

			if (capabilitySetting is StorageCapabilitySetting storageCapabilitySetting)
			{
				capabilitySettings.Remove(storageCapabilitySetting);
			}

			return this;
		}

		/// <summary>
		/// Configures the script execution capabilities using the specified collection of capability settings.
		/// </summary>
		/// <param name="capabilitySettings">A collection of <see cref="CapabilitySetting"/> objects that define the capabilities to be applied. Cannot
		/// be null.</param>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance with the updated capability settings.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySettings"/> is null.</exception>
		public ScriptExecutionDetails SetCapabilities(IEnumerable<CapabilitySetting> capabilitySettings)
		{
			if (capabilitySettings == null)
			{
				throw new ArgumentNullException(nameof(capabilitySettings));
			}

			this.capabilitySettings.Clear();
			foreach (var setting in capabilitySettings)
				AddCapability(setting);

			return this;
		}

		/// <summary>
		/// Adds a new capacity.
		/// </summary>
		/// <param name="capacitySetting">The capacity setting to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails AddCapacity(CapacitySetting capacitySetting)
		{
			if (capacitySetting == null)
			{
				throw new ArgumentNullException(nameof(capacitySetting));
			}

			if (capacitySetting is NumberCapacitySetting numberCapacity)
			{
				numberCapacitySettings.Add(new StorageNumberCapacitySetting(numberCapacity));
			}
			else if (capacitySetting is RangeCapacitySetting rangeCapacity)
			{
				rangeCapacitySettings.Add(new StorageRangeCapacitySetting(rangeCapacity));
			}
			else
			{
				throw new ArgumentException("The capacity setting type is not supported.", nameof(capacitySetting));
			}

			return this;
		}

		/// <summary>
		/// Removes the specified capacity.
		/// </summary>
		/// <param name="capacitySetting">The capacity to remove. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails RemoveCapacity(CapacitySetting capacitySetting)
		{
			if (capacitySetting == null)
			{
				throw new ArgumentNullException(nameof(capacitySetting));
			}

			if (capacitySetting is StorageNumberCapacitySetting storageNumberCapacitySetting)
			{
				numberCapacitySettings.Remove(storageNumberCapacitySetting);
			}
			else if (capacitySetting is StorageRangeCapacitySetting storageRangeCapacitySetting)
			{
				rangeCapacitySettings.Remove(storageRangeCapacitySetting);
			}

			return this;
		}

		/// <summary>
		/// Configures the script execution capacities using the specified collection of capacity settings.
		/// </summary>
		/// <param name="capacitySettings">A collection of <see cref="CapacitySetting"/> objects that define the capacities to be applied. Cannot
		/// be null.</param>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance with the updated capacity settings.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySettings"/> is null.</exception>
		public ScriptExecutionDetails SetCapacities(IEnumerable<CapacitySetting> capacitySettings)
		{
			if (capacitySettings == null)
			{
				throw new ArgumentNullException(nameof(capacitySettings));
			}

			this.numberCapacitySettings.Clear();
			this.rangeCapacitySettings.Clear();

			foreach (var setting in capacitySettings)
				AddCapacity(setting);

			return this;
		}

		/// <summary>
		/// Adds a new configuration.
		/// </summary>
		/// <param name="configurationSetting">The configuration setting to add.</param>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSetting"/> is <see langword="null"/>.</exception>
		public ScriptExecutionDetails AddConfiguration(ConfigurationSetting configurationSetting)
		{
			if (configurationSetting == null)
			{
				throw new ArgumentNullException(nameof(configurationSetting));
			}

			if (configurationSetting is TextConfigurationSetting textConfiguration)
			{
				textConfigurationSettings.Add(new StorageTextConfigurationSetting(textConfiguration));
			}
			else if (configurationSetting is NumberConfigurationSetting numberConfiguration)
			{
				numberConfigurationSettings.Add(new StorageNumberConfigurationSetting(numberConfiguration));
			}
			else if (configurationSetting is DiscreteTextConfigurationSetting discreteTextConfiguration)
			{
				discreteTextConfigurationSettings.Add(new StorageDiscreteTextConfigurationSetting(discreteTextConfiguration));
			}
			else if (configurationSetting is DiscreteNumberConfigurationSetting discreteNumberConfiguration)
			{
				discreteNumberConfigurationSettings.Add(new StorageDiscreteNumberConfigurationSetting(discreteNumberConfiguration));
			}
			else
			{
				throw new ArgumentException("The configuration setting type is not supported.", nameof(configurationSetting));
			}

			return this;
		}

		/// <summary>
		/// Removes the specified configuration.
		/// </summary>
		/// <param name="configurationSetting">The configuration to remove. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSetting"/> is <see langword="null"/>.</exception>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance.</returns>
		public ScriptExecutionDetails RemoveConfiguration(ConfigurationSetting configurationSetting)
		{
			if (configurationSetting == null)
			{
				throw new ArgumentNullException(nameof(configurationSetting));
			}

			if (configurationSetting is StorageTextConfigurationSetting storageTextConfigurationSetting)
			{
				textConfigurationSettings.Remove(storageTextConfigurationSetting);
			}
			else if (configurationSetting is StorageNumberConfigurationSetting storageNumberConfigurationSetting)
			{
				numberConfigurationSettings.Remove(storageNumberConfigurationSetting);
			}
			else if (configurationSetting is StorageDiscreteTextConfigurationSetting storageDiscreteTextConfigurationSetting)
			{
				discreteTextConfigurationSettings.Remove(storageDiscreteTextConfigurationSetting);
			}
			else if (configurationSetting is StorageDiscreteNumberConfigurationSetting storageDiscreteNumberConfigurationSetting)
			{
				discreteNumberConfigurationSettings.Remove(storageDiscreteNumberConfigurationSetting);
			}

			return this;
		}

		/// <summary>
		/// Configures the script execution configurations using the specified collection of configuration settings.
		/// </summary>
		/// <param name="configurationSettings">A collection of <see cref="ConfigurationSetting"/> objects that define the configurations to be applied. Cannot
		/// be null.</param>
		/// <returns>The current <see cref="ScriptExecutionDetails"/> instance with the updated configuration settings.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSettings"/> is null.</exception>
		public ScriptExecutionDetails SetConfigurations(IEnumerable<ConfigurationSetting> configurationSettings)
		{
			if (configurationSettings == null)
			{
				throw new ArgumentNullException(nameof(configurationSettings));
			}

			this.textConfigurationSettings.Clear();
			this.numberConfigurationSettings.Clear();
			this.discreteTextConfigurationSettings.Clear();
			this.discreteNumberConfigurationSettings.Clear();

			foreach (var setting in configurationSettings)
				AddConfiguration(setting);

			return this;
		}

		/// <summary>
		/// Checks if the provided object is an ScriptExecutionDetails instance and compares its properties to determine equality.
		/// </summary>
		/// <param name="obj">Object to compare.</param>
		/// <returns>True, if properties match, else false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not ScriptExecutionDetails other)
			{
				return false;
			}

			return ScriptName == other.ScriptName &&
					 scriptElementSettings.ScrambledEquals(other.scriptElementSettings) &&
					 scriptParameterSettings.ScrambledEquals(other.scriptParameterSettings) &&
					 capabilitySettings.ScrambledEquals(other.capabilitySettings) &&
					 numberCapacitySettings.ScrambledEquals(other.numberCapacitySettings) &&
					 rangeCapacitySettings.ScrambledEquals(other.rangeCapacitySettings) &&
					 textConfigurationSettings.ScrambledEquals(other.textConfigurationSettings) &&
					 numberConfigurationSettings.ScrambledEquals(other.numberConfigurationSettings) &&
					 discreteTextConfigurationSettings.ScrambledEquals(other.discreteTextConfigurationSettings) &&
					 discreteNumberConfigurationSettings.ScrambledEquals(other.discreteNumberConfigurationSettings);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (ScriptName != null ? ScriptName.GetHashCode() : 0);

				foreach (var elementSettings in scriptElementSettings.OrderBy(x => x.Name))
				{
					hash = (hash * 23) + elementSettings.GetHashCode();
				}

				foreach (var parameterSettings in scriptParameterSettings.OrderBy(x => x.Name))
				{
					hash = (hash * 23) + parameterSettings.GetHashCode();
				}

				foreach (var capabilitySetting in capabilitySettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + capabilitySetting.GetHashCode();
				}

				foreach (var capacitySetting in numberCapacitySettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + capacitySetting.GetHashCode();
				}

				foreach (var capacitySetting in rangeCapacitySettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + capacitySetting.GetHashCode();
				}

				foreach (var configurationSetting in textConfigurationSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + configurationSetting.GetHashCode();
				}

				foreach (var configurationSetting in numberConfigurationSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + configurationSetting.GetHashCode();
				}

				foreach (var configurationSetting in discreteTextConfigurationSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + configurationSetting.GetHashCode();
				}

				foreach (var configurationSetting in discreteNumberConfigurationSettings.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + configurationSetting.GetHashCode();
				}

				return hash;
			}
		}

		internal static ScriptExecutionDetails FromStorage(MediaOpsPlanApi planApi, Storage.DOM.ScriptExecutionDetails storageScriptExecutionDetails)
		{
			if (storageScriptExecutionDetails == null)
			{
				throw new ArgumentNullException(nameof(storageScriptExecutionDetails));
			}

			if (string.IsNullOrEmpty(storageScriptExecutionDetails.ScriptName))
			{
				return null;
			}

			var scriptExecutionDetails = new ScriptExecutionDetails(storageScriptExecutionDetails.ScriptName);

			scriptExecutionDetails.ParseStorageDummies(storageScriptExecutionDetails.Dummies, storageScriptExecutionDetails.DummyReferences);
			scriptExecutionDetails.ParseStorageParameters(storageScriptExecutionDetails.Parameters, storageScriptExecutionDetails.ParameterReferences);
			scriptExecutionDetails.ParseStorageProfileParameterValues(planApi, storageScriptExecutionDetails.ProfileParameterValues);

			return scriptExecutionDetails;
		}

		internal Storage.DOM.ScriptExecutionDetails ToStorage()
		{
			var storageScriptExecutionDetails = new Storage.DOM.ScriptExecutionDetails
			{
				ScriptName = ScriptName,
			};

			foreach (var scriptElementSetting in scriptElementSettings)
			{
				if (scriptElementSetting.Reference != null)
				{
					storageScriptExecutionDetails.DummyReferences.Add(scriptElementSetting.Name, scriptElementSetting.Reference.ToStorage());
				}
				else
				{
					storageScriptExecutionDetails.Dummies.Add(scriptElementSetting.Name, (scriptElementSetting.DmsElementId == default) ? scriptElementSetting.ElementName : scriptElementSetting.DmsElementId.Value);
				}
			}

			foreach (var scriptParameterSetting in scriptParameterSettings)
			{
				if (scriptParameterSetting.Reference != null)
				{
					storageScriptExecutionDetails.ParameterReferences.Add(scriptParameterSetting.Name, scriptParameterSetting.Reference.ToStorage());
				}
				else
				{
					storageScriptExecutionDetails.Parameters.Add(scriptParameterSetting.Name, scriptParameterSetting.Value);
				}
			}

			foreach (var capabilitySetting in capabilitySettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(capabilitySetting.GetProfileParameterValueWithChanges());
			}

			foreach (var capacitySetting in numberCapacitySettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(capacitySetting.GetProfileParameterValueWithChanges());
			}

			foreach (var capacitySetting in rangeCapacitySettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(capacitySetting.GetProfileParameterValueWithChanges());
			}

			foreach (var configurationSetting in textConfigurationSettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(configurationSetting.GetProfileParameterValueWithChanges());
			}

			foreach (var configurationSetting in numberConfigurationSettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(configurationSetting.GetProfileParameterValueWithChanges());
			}

			foreach (var configurationSetting in discreteTextConfigurationSettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(configurationSetting.GetProfileParameterValueWithChanges());
			}

			foreach (var configurationSetting in discreteNumberConfigurationSettings)
			{
				storageScriptExecutionDetails.ProfileParameterValues.Add(configurationSetting.GetProfileParameterValueWithChanges());
			}

			return storageScriptExecutionDetails;
		}

		private void ParseStorageDummies(Dictionary<string, string> storageDummies, Dictionary<string, Storage.DOM.DataReference> dummyReferences)
		{
			foreach (var kvp in storageDummies)
			{
				var scriptElementSetting = new ScriptElementSetting(kvp.Key);

				if (kvp.Value.Contains('/'))
				{
					scriptElementSetting.DmsElementId = new DmsElementId(kvp.Value);
				}
				else
				{
					scriptElementSetting.ElementName = kvp.Value;
				}

				AddScriptElement(scriptElementSetting);
			}

			foreach (var kvp in dummyReferences)
			{
				var scriptElementSetting = new ScriptElementSetting(kvp.Key)
				{
					Reference = DataReference.FromStorage(kvp.Value),
				};

				AddScriptElement(scriptElementSetting);
			}
		}

		private void ParseStorageParameters(Dictionary<string, string> storageParameters, Dictionary<string, Storage.DOM.DataReference> parameterReferences)
		{
			foreach (var kvp in storageParameters)
			{
				var scriptParameterSetting = new ScriptParameterSetting(kvp.Key)
				{
					Value = kvp.Value,
				};

				AddScriptParameter(scriptParameterSetting);
			}

			foreach (var kvp in parameterReferences)
			{
				var scriptParameterSetting = new ScriptParameterSetting(kvp.Key)
				{
					Reference = DataReference.FromStorage(kvp.Value),
				};

				AddScriptParameter(scriptParameterSetting);
			}
		}

		private void ParseStorageProfileParameterValues(MediaOpsPlanApi planApi, List<ProfileParameterValue> profileParameterValues)
		{
			if (profileParameterValues == null || profileParameterValues.Count == 0)
			{
				return;
			}

			var parameterIds = profileParameterValues.Select(ppv => ppv.ProfileParameterId).Distinct();
			var parametersById = planApi.CoreHelpers.ProfileProvider.GetParametersById(parameterIds).ToDictionary(x => x.ID);

			foreach (var profileParameterValue in profileParameterValues)
			{
				if (!parametersById.TryGetValue(profileParameterValue.ProfileParameterId, out var profileParameter))
				{
					planApi.Logger.Information(this, $"ScriptExecutionDetails > ParseStorageProfileParameterValues > Profile parameter with ID '{profileParameterValue.ProfileParameterId}' not found.");
					continue;
				}

				if (profileParameter.IsCapability())
				{
					capabilitySettings.Add(new StorageCapabilitySetting(profileParameterValue));
				}
				else if (profileParameter.IsCapacity())
				{
					ParseStorageProfileParameterValues_Capacity(profileParameter, profileParameterValue);
				}
				else if (profileParameter.IsConfiguration())
				{
					ParseStorageProfileParameterValues_Configuration(profileParameter, profileParameterValue);
				}
			}
		}

		private void ParseStorageProfileParameterValues_Capacity(Net.Profiles.Parameter profileParameter, ProfileParameterValue profileParameterValue)
		{
			if (profileParameter.IsRange())
			{
				rangeCapacitySettings.Add(new StorageRangeCapacitySetting(profileParameterValue));
			}
			else
			{
				numberCapacitySettings.Add(new StorageNumberCapacitySetting(profileParameterValue));
			}
		}

		private void ParseStorageProfileParameterValues_Configuration(Net.Profiles.Parameter profileParameter, ProfileParameterValue profileParameterValue)
		{
			if (profileParameter.IsText())
			{
				textConfigurationSettings.Add(new StorageTextConfigurationSetting(profileParameterValue));
			}
			else if (profileParameter.IsNumber())
			{
				numberConfigurationSettings.Add(new StorageNumberConfigurationSetting(profileParameterValue));
			}
			else if (profileParameter.IsTextDiscreet())
			{
				discreteTextConfigurationSettings.Add(new StorageDiscreteTextConfigurationSetting(new DiscreteTextConfiguration(profileParameter), profileParameterValue));
			}
			else if (profileParameter.IsNumberDiscreet())
			{
				discreteNumberConfigurationSettings.Add(new StorageDiscreteNumberConfigurationSetting(new DiscreteNumberConfiguration(profileParameter), profileParameterValue));
			}
		}
	}
}
