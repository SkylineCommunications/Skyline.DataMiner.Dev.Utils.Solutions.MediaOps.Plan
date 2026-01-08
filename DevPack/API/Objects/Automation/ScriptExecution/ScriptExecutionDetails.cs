namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

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
        /// Initializes a new instance of the ScriptExecutionDetails class with the specified script name.
        /// </summary>
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
        public IReadOnlyCollection<ScriptElementSetting> ScriptElementSettings => scriptElementSettings;

        /// <summary>
        /// Gets the collection of script parameter settings.
        /// </summary>
        public IReadOnlyCollection<ScriptParameterSetting> ScriptParameterSettings => scriptParameterSettings;

        /// <summary>
        /// Gets the collection of capability settings.
        /// </summary>
        public IReadOnlyCollection<CapabilitySetting> CapabilitySettings => capabilitySettings;

        /// <summary>
        /// Gets the collection of capacity settings.
        /// </summary>
        public IReadOnlyCollection<CapacitySetting> CapacitySettings => numberCapacitySettings.Concat<CapacitySetting>(rangeCapacitySettings).ToList();

        /// <summary>
        /// Gets the collection of configuration settings.
        /// </summary>
        public IReadOnlyCollection<ConfigurationSetting> ConfigurationSettings
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
        /// Adds a new script dummy.
        /// </summary>
        /// <param name="scriptDummySetting">The script dummy to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptDummySetting"/> is <see langword="null"/>.</exception>
        public ScriptExecutionDetails AddScriptDummy(ScriptElementSetting scriptDummySetting)
        {
            if (scriptDummySetting == null)
            {
                throw new ArgumentNullException(nameof(scriptDummySetting));
            }

            scriptElementSettings.Add(scriptDummySetting);
            return this;
        }

        /// <summary>
        /// Removes the specified script dummy.
        /// </summary>
        /// <param name="scriptDummySetting">The script dummy to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptDummySetting"/> is <see langword="null"/>.</exception>
        public ScriptExecutionDetails RemoveScriptDummy(ScriptElementSetting scriptDummySetting)
        {
            if (scriptDummySetting == null)
            {
                throw new ArgumentNullException(nameof(scriptDummySetting));
            }

            scriptElementSettings.Remove(scriptDummySetting);
            return this;
        }

        /// <summary>
        /// Adds a new script parameter.
        /// </summary>
        /// <param name="scriptParameterSetting">The script parameter to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptParameterSetting"/> is <see langword="null"/>.</exception>
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
        /// Adds a new capacity.
        /// </summary>
        /// <param name="capacitySetting">The capacity setting to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
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
        /// Adds a new configuration.
        /// </summary>
        /// <param name="configurationSetting">The configuration setting to add.</param>
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

        internal static ScriptExecutionDetails FromStorage(MediaOpsPlanApi planApi, Storage.DOM.ScriptExecutionDetails storageScriptExecutionDetails)
        {
            if (storageScriptExecutionDetails == null)
            {
                throw new ArgumentNullException(nameof(storageScriptExecutionDetails));
            }

            var scriptExecutionDetails = new ScriptExecutionDetails(storageScriptExecutionDetails.ScriptName);

            scriptExecutionDetails.ParseStorageDummies(storageScriptExecutionDetails.Dummies);
            scriptExecutionDetails.ParseStorageParameters(storageScriptExecutionDetails.Parameters);
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
                storageScriptExecutionDetails.Dummies.Add(scriptElementSetting.Name, (scriptElementSetting.DmsElementId == null) ? scriptElementSetting.ElementName : scriptElementSetting.DmsElementId.Value);
            }

            foreach (var scriptParameterSetting in scriptParameterSettings)
            {
                storageScriptExecutionDetails.Parameters.Add(scriptParameterSetting.Name, scriptParameterSetting.Value);
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

        private void ParseStorageDummies(Dictionary<string, string> storageDummies)
        {
            foreach (var kvp in storageDummies)
            {
                var scriptElementSetting = new ScriptElementSetting(kvp.Key);

                if (kvp.Value.Contains('/'))
                {
                    scriptElementSetting.DmsElementId = new Core.DataMinerSystem.Common.DmsElementId(kvp.Value);
                }
                else
                {
                    scriptElementSetting.ElementName = kvp.Value;
                }

                AddScriptDummy(scriptElementSetting);
            }
        }

        private void ParseStorageParameters(Dictionary<string, string> storageParameters)
        {
            foreach (var kvp in storageParameters)
            {
                var scriptParameterSetting = new ScriptParameterSetting(kvp.Key)
                {
                    Value = kvp.Value
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
                    planApi.Logger.LogInformation($"ScriptExecutionDetails > ParseStorageProfileParameterValues > Profile parameter with ID '{profileParameterValue.ProfileParameterId}' not found.");
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
