namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioOrchestrationSettings : OrchestrationSettings
    {
        private readonly List<ResourceStudioCapabilitySetting> capabilitySettings = [];

        private readonly List<ResourceStudioNumberCapacitySetting> numberCapacitySettings = [];

        private readonly List<ResourceStudioRangeCapacitySetting> rangeCapacitySettings = [];

        private readonly List<ResourceStudioTextConfigurationSetting> textConfigurationSettings = [];

        private readonly List<ResourceStudioNumberConfigurationSetting> numberConfigurationSettings = [];

        private readonly List<ResourceStudioDiscreteTextConfigurationSetting> discreteTextConfigurationSettings = [];

        private readonly List<ResourceStudioDiscreteNumberConfigurationSetting> discreteNumberConfigurationSettings = [];

        private readonly List<ResourceStudioOrchestrationEvent> orchestrationEvents = [];

        internal ResourceStudioOrchestrationSettings() : base()
        {
        }

        internal ResourceStudioOrchestrationSettings(MediaOpsPlanApi planApi, StorageResourceStudio.ConfigurationInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(planApi, instance);
        }

        public override IReadOnlyCollection<OrchestrationEvent> OrchestrationEvents => orchestrationEvents;

        public override IReadOnlyCollection<CapabilitySetting> Capabilities => capabilitySettings;

        public override IReadOnlyCollection<CapacitySetting> Capacities => numberCapacitySettings.Concat<CapacitySetting>(rangeCapacitySettings).ToList();

        public override IReadOnlyCollection<ConfigurationSetting> Configurations
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

        public override OrchestrationSettings AddCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            capabilitySettings.Add(new ResourceStudioCapabilitySetting(capabilitySetting));
            return this;
        }

        public override OrchestrationSettings AddCapacity(CapacitySetting capacitySetting)
        {
            if (capacitySetting == null)
            {
                throw new ArgumentNullException(nameof(capacitySetting));
            }

            if (capacitySetting is NumberCapacitySetting numberCapacity)
            {
                numberCapacitySettings.Add(new ResourceStudioNumberCapacitySetting(numberCapacity));
            }
            else if (capacitySetting is RangeCapacitySetting rangeCapacity)
            {
                rangeCapacitySettings.Add(new ResourceStudioRangeCapacitySetting(rangeCapacity));
            }
            else
            {
                throw new ArgumentException("The capacity setting type is not supported.", nameof(capacitySetting));
            }

            return this;
        }

        public override OrchestrationSettings AddConfiguration(ConfigurationSetting configurationSetting)
        {
            if (configurationSetting == null)
            {
                throw new ArgumentNullException(nameof(configurationSetting));
            }

            if (configurationSetting is TextConfigurationSetting textConfiguration)
            {
                textConfigurationSettings.Add(new ResourceStudioTextConfigurationSetting(textConfiguration));
            }
            else if (configurationSetting is NumberConfigurationSetting numberConfiguration)
            {
                numberConfigurationSettings.Add(new ResourceStudioNumberConfigurationSetting(numberConfiguration));
            }
            else if (configurationSetting is DiscreteTextConfigurationSetting discreteTextConfiguration)
            {
                discreteTextConfigurationSettings.Add(new ResourceStudioDiscreteTextConfigurationSetting(discreteTextConfiguration));
            }
            else if (configurationSetting is DiscreteNumberConfigurationSetting discreteNumberConfiguration)
            {
                discreteNumberConfigurationSettings.Add(new ResourceStudioDiscreteNumberConfigurationSetting(discreteNumberConfiguration));
            }
            else
            {
                throw new ArgumentException("The configuration setting type is not supported.", nameof(configurationSetting));
            }

            return this;
        }

        public override OrchestrationSettings AddOrchestrationEvent(OrchestrationEvent orchestrationEvent)
        {
            throw new NotImplementedException();
        }

        public override OrchestrationSettings RemoveCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            if (capabilitySetting.OriginalSection == null)
            {
                return this;
            }

            var toRemove = capabilitySettings.SingleOrDefault(x => x.OriginalSection.ID == capabilitySetting.OriginalSection.ID);
            if (toRemove == null)
            {
                return this;
            }

            capabilitySettings.Remove(toRemove);
            return this;
        }

        public override OrchestrationSettings RemoveCapacity(CapacitySetting capacitySetting)
        {
            if (capacitySetting == null)
            {
                throw new ArgumentNullException(nameof(capacitySetting));
            }

            if (capacitySetting.OriginalSection == null)
            {
                return this;
            }

            if (capacitySetting is NumberCapacitySetting)
            {
                var toRemoveNumber = numberCapacitySettings.SingleOrDefault(x => x.OriginalSection.ID == capacitySetting.OriginalSection.ID);
                if (toRemoveNumber != null)
                {
                    numberCapacitySettings.Remove(toRemoveNumber);
                }
            }
            else if (capacitySetting is RangeCapacitySetting)
            {
                var toRemoveRange = rangeCapacitySettings.SingleOrDefault(x => x.OriginalSection.ID == capacitySetting.OriginalSection.ID);
                if (toRemoveRange != null)
                {
                    rangeCapacitySettings.Remove(toRemoveRange);
                }
            }

            return this;
        }

        public override OrchestrationSettings RemoveConfiguration(ConfigurationSetting configurationSetting)
        {
            if (configurationSetting == null)
            {
                throw new ArgumentNullException(nameof(configurationSetting));
            }

            if (configurationSetting.OriginalSection == null)
            {
                return this;
            }

            if (configurationSetting is TextConfigurationSetting)
            {
                var toRemove = textConfigurationSettings.SingleOrDefault(x => x.OriginalSection.ID == configurationSetting.OriginalSection.ID);
                if (toRemove != null)
                {
                    textConfigurationSettings.Remove(toRemove);
                }
            }
            else if (configurationSetting is NumberConfigurationSetting)
            {
                var toRemove = numberConfigurationSettings.SingleOrDefault(x => x.OriginalSection.ID == configurationSetting.OriginalSection.ID);
                if (toRemove != null)
                {
                    numberConfigurationSettings.Remove(toRemove);
                }
            }
            else if (configurationSetting is DiscreteTextConfigurationSetting)
            {
                var toRemove = discreteTextConfigurationSettings.SingleOrDefault(x => x.OriginalSection.ID == configurationSetting.OriginalSection.ID);
                if (toRemove != null)
                {
                    discreteTextConfigurationSettings.Remove(toRemove);
                }
            }
            else if (configurationSetting is DiscreteNumberConfigurationSetting)
            {
                var toRemove = discreteNumberConfigurationSettings.SingleOrDefault(x => x.OriginalSection.ID == configurationSetting.OriginalSection.ID);
                if (toRemove != null)
                {
                    discreteNumberConfigurationSettings.Remove(toRemove);
                }
            }

            return this;
        }

        public override OrchestrationSettings RemoveOrchestrationEvent(OrchestrationEvent orchestrationEvent)
        {
            throw new NotImplementedException();
        }

        private void ParseInstance(MediaOpsPlanApi planApi, StorageResourceStudio.ConfigurationInstance instance)
        {
            ParseParameterValues(planApi, instance.ProfileParameterValues);
        }

        private void ParseParameterValues(MediaOpsPlanApi planApi, IList<StorageResourceStudio.ProfileParameterValuesSection> parameterValues)
        {
            if (parameterValues == null || parameterValues.Count == 0)
            {
                return;
            }

            var parameterIds = parameterValues.Select(pv => pv.ProfileParameterId).Distinct();
            var parametersById = planApi.CoreHelpers.ProfileProvider.GetParametersById(parameterIds).ToDictionary(x => x.ID);

            foreach (var section in parameterValues)
            {
                if (!parametersById.TryGetValue(section.ProfileParameterId, out var profileParameter))
                {
                    continue;
                }

                if (profileParameter.IsCapability())
                {
                    capabilitySettings.Add(new ResourceStudioCapabilitySetting(section));
                }
                else if (profileParameter.IsCapacity())
                {
                    ParseParameterValues_Capacity(profileParameter, section);
                }
                else if (profileParameter.IsConfiguration())
                {
                    ParseParameterValues_Configuration(profileParameter, section);
                }
            }
        }

        private void ParseParameterValues_Capacity(Net.Profiles.Parameter profileParameter, StorageResourceStudio.ProfileParameterValuesSection section)
        {
            if (profileParameter.IsRange())
            {
                rangeCapacitySettings.Add(new ResourceStudioRangeCapacitySetting(section));
            }
            else
            {
                numberCapacitySettings.Add(new ResourceStudioNumberCapacitySetting(section));
            }
        }

        private void ParseParameterValues_Configuration(Net.Profiles.Parameter profileParameter, StorageResourceStudio.ProfileParameterValuesSection section)
        {
            if (profileParameter.IsText())
            {
                textConfigurationSettings.Add(new ResourceStudioTextConfigurationSetting(section));
            }
            else if (profileParameter.IsNumber())
            {
                numberConfigurationSettings.Add(new ResourceStudioNumberConfigurationSetting(section));
            }
            else if (profileParameter.IsTextDiscreet())
            {
                discreteTextConfigurationSettings.Add(new ResourceStudioDiscreteTextConfigurationSetting(section));
            }
            else if (profileParameter.IsNumberDiscreet())
            {
                discreteNumberConfigurationSettings.Add(new ResourceStudioDiscreteNumberConfigurationSetting(section));
            }
        }
    }
}
