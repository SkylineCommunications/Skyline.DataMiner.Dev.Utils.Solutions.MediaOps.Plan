namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioOrchestrationSettings : OrchestrationSettings
    {
        private readonly List<ResourceStudioCapabilitySetting> capabilitySettings = [];
        private readonly List<ResourceStudioDiscreteNumberConfigurationSetting> discreteNumberConfigurationSettings = [];
        private readonly List<ResourceStudioDiscreteTextConfigurationSetting> discreteTextConfigurationSettings = [];
        private readonly List<ResourceStudioNumberCapacitySetting> numberCapacitySettings = [];
        private readonly List<ResourceStudioNumberConfigurationSetting> numberConfigurationSettings = [];
        private readonly List<ResourceStudioOrchestrationEvent> orchestrationEvents = [];
        private readonly List<ResourceStudioRangeCapacitySetting> rangeCapacitySettings = [];
        private readonly List<ResourceStudioTextConfigurationSetting> textConfigurationSettings = [];
        private StorageResourceStudio.ConfigurationInstance originalInstance;

        private StorageResourceStudio.ConfigurationInstance updatedInstance;
        internal ResourceStudioOrchestrationSettings() : base()
        {
        }

        internal ResourceStudioOrchestrationSettings(MediaOpsPlanApi planApi, StorageResourceStudio.ConfigurationInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(planApi, instance);
            InitTracking();
        }

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

        public override IReadOnlyCollection<OrchestrationEvent> OrchestrationEvents => orchestrationEvents;
        internal StorageResourceStudio.ConfigurationInstance OriginalInstance => originalInstance;

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
            if (orchestrationEvent == null)
            {
                throw new ArgumentNullException(nameof(orchestrationEvent));
            }

            orchestrationEvents.Add(new ResourceStudioOrchestrationEvent(orchestrationEvent));
            return this;
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
            if (orchestrationEvent == null)
            {
                throw new ArgumentNullException(nameof(orchestrationEvent));
            }

            orchestrationEvents.RemoveAll(x => x.Equals(orchestrationEvent));
            return this;
        }

        public override OrchestrationSettings SetCapabilities(IEnumerable<CapabilitySetting> capabilitySettings)
        {
            if (capabilitySettings == null)
            {
                throw new ArgumentNullException(nameof(capabilitySettings));
            }

            this.capabilitySettings.Clear();
            foreach (var capabilitySetting in capabilitySettings)
            {
                AddCapability(capabilitySetting);
            }

            return this;
        }

        public override OrchestrationSettings SetCapacities(IEnumerable<CapacitySetting> capacitySettings)
        {
            if (capacitySettings == null)
            {
                throw new ArgumentNullException(nameof(capacitySettings));
            }

            this.numberCapacitySettings.Clear();
            this.rangeCapacitySettings.Clear();

            foreach (var capacitySetting in capacitySettings)
            {
                AddCapacity(capacitySetting);
            }

            return this;
        }

        public override OrchestrationSettings SetConfigurations(IEnumerable<ConfigurationSetting> configurationSettings)
        {
            if (configurationSettings == null)
            {
                throw new ArgumentNullException(nameof(configurationSettings));
            }

            this.textConfigurationSettings.Clear();
            this.numberConfigurationSettings.Clear();
            this.discreteTextConfigurationSettings.Clear();
            this.discreteNumberConfigurationSettings.Clear();

            foreach (var configurationSetting in configurationSettings)
            {
                AddConfiguration(configurationSetting);
            }

            return this;
        }

        public override OrchestrationSettings SetOrchestrationEvents(IEnumerable<OrchestrationEvent> orchestrationEvents)
        {
            if (orchestrationEvents == null)
            {
                throw new ArgumentNullException(nameof(orchestrationEvents));
            }

            this.orchestrationEvents.Clear();
            foreach (var orchestrationEvent in orchestrationEvents)
            {
                AddOrchestrationEvent(orchestrationEvent);
            }

            return this;
        }

        internal StorageResourceStudio.ConfigurationInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ConfigurationInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.ProfileParameterValues.Clear();
            foreach (var capability in capabilitySettings)
            {
                updatedInstance.ProfileParameterValues.Add(capability.GetSectionWithChanges());
            }
            foreach (var capacity in numberCapacitySettings)
            {
                updatedInstance.ProfileParameterValues.Add(capacity.GetSectionWithChanges());
            }
            foreach (var capacity in rangeCapacitySettings)
            {
                updatedInstance.ProfileParameterValues.Add(capacity.GetSectionWithChanges());
            }
            foreach (var configuration in textConfigurationSettings)
            {
                updatedInstance.ProfileParameterValues.Add(configuration.GetSectionWithChanges());
            }
            foreach (var configuration in numberConfigurationSettings)
            {
                updatedInstance.ProfileParameterValues.Add(configuration.GetSectionWithChanges());
            }
            foreach (var configuration in discreteTextConfigurationSettings)
            {
                updatedInstance.ProfileParameterValues.Add(configuration.GetSectionWithChanges());
            }
            foreach (var configuration in discreteNumberConfigurationSettings)
            {
                updatedInstance.ProfileParameterValues.Add(configuration.GetSectionWithChanges());
            }

            updatedInstance.OrchestrationEvents.Clear();
            foreach (var orchestrationEvent in orchestrationEvents)
            {
                updatedInstance.OrchestrationEvents.Add(orchestrationEvent.GetSectionWithChanges());
            }

            return updatedInstance;
        }

        private void ParseInstance(MediaOpsPlanApi planApi, StorageResourceStudio.ConfigurationInstance instance)
        {
            originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            ParseParameterValues(planApi, instance.ProfileParameterValues);
            ParseOrchestrationEvents(planApi, instance.OrchestrationEvents);
        }

        private void ParseOrchestrationEvents(MediaOpsPlanApi planApi, IList<StorageResourceStudio.OrchestrationEventsSection> orchestrationEvents)
        {
            if (orchestrationEvents == null || orchestrationEvents.Count == 0)
            {
                return;
            }

            foreach (var orchestrationEvent in orchestrationEvents)
            {
                this.orchestrationEvents.Add(new ResourceStudioOrchestrationEvent(planApi, orchestrationEvent));
            }
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
                    planApi.Logger.LogInformation(this, $"ResourceStudioOrchestrationSettings > ParseParameterValues > Profile parameter with ID '{section.ProfileParameterId}' not found.");
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
                discreteTextConfigurationSettings.Add(new ResourceStudioDiscreteTextConfigurationSetting(new DiscreteTextConfiguration(profileParameter), section));
            }
            else if (profileParameter.IsNumberDiscreet())
            {
                discreteNumberConfigurationSettings.Add(new ResourceStudioDiscreteNumberConfigurationSetting(new DiscreteNumberConfiguration(profileParameter), section));
            }
        }
    }
}
