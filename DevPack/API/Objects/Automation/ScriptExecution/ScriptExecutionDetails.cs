namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the configuration and settings required to execute a script.
    /// </summary>
    public class ScriptExecutionDetails
    {
        private readonly List<ScriptDummySetting> scriptDummySettings = [];

        private readonly List<ScriptParameterSetting> scriptParameterSettings = [];

        private readonly List<CapabilitySetting> capabilitySettings = [];

        private readonly List<CapacitySetting> capacitySettings = [];

        private readonly List<ConfigurationSetting> configurationSettings = [];

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
        /// Gets the collection of script dummy settings.
        /// </summary>
        public IReadOnlyCollection<ScriptDummySetting> ScriptDummySettings => scriptDummySettings;

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
        public IReadOnlyCollection<CapacitySetting> CapacitySettings => capacitySettings;

        /// <summary>
        /// Gets the collection of configuration settings.
        /// </summary>
        public IReadOnlyCollection<ConfigurationSetting> ConfigurationSettings => configurationSettings;

        /// <summary>
        /// Adds a new script dummy.
        /// </summary>
        /// <param name="scriptDummySetting">The script dummy to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptDummySetting"/> is <see langword="null"/>.</exception>
        public ScriptExecutionDetails AddScriptDummy(ScriptDummySetting scriptDummySetting)
        {
            if (scriptDummySetting == null)
            {
                throw new ArgumentNullException(nameof(scriptDummySetting));
            }

            scriptDummySettings.Add(scriptDummySetting);
            return this;
        }

        /// <summary>
        /// Removes the specified script dummy.
        /// </summary>
        /// <param name="scriptDummySetting">The script dummy to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="scriptDummySetting"/> is <see langword="null"/>.</exception>
        public ScriptExecutionDetails RemoveScriptDummy(ScriptDummySetting scriptDummySetting)
        {
            if (scriptDummySetting == null)
            {
                throw new ArgumentNullException(nameof(scriptDummySetting));
            }

            scriptDummySettings.Remove(scriptDummySetting);
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

            capabilitySettings.Add(capabilitySetting);
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

            capabilitySettings.Remove(capabilitySetting);
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

            capacitySettings.Add(capacitySetting);
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

            capacitySettings.Remove(capacitySetting);
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

            configurationSettings.Add(configurationSetting);
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

            configurationSettings.Remove(configurationSetting);
            return this;
        }
    }
}
