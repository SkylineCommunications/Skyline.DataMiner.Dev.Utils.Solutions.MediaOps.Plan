namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using StorageWorkflow = Storage.DOM.SlcWorkflow;

    internal class WorkflowOrchestrationSettings : OrchestrationSettings
    {
        private readonly List<WorkflowCapabilitySetting> capabilitySettings = [];
        private readonly List<WorkflowDiscreteNumberConfigurationSetting> discreteNumberConfigurationSettings = [];
        private readonly List<WorkflowDiscreteTextConfigurationSetting> discreteTextConfigurationSettings = [];
        private readonly List<WorkflowNumberCapacitySetting> numberCapacitySettings = [];
        private readonly List<WorkflowNumberConfigurationSetting> numberConfigurationSettings = [];
        private readonly List<WorkflowOrchestrationEvent> orchestrationEvents = [];
        private readonly List<WorkflowRangeCapacitySetting> rangeCapacitySettings = [];
        private readonly List<WorkflowTextConfigurationSetting> textConfigurationSettings = [];

        private StorageWorkflow.ConfigurationInstance originalInstance;

        private StorageWorkflow.ConfigurationInstance updatedInstance;

        internal WorkflowOrchestrationSettings() : base()
        {
        }

        internal WorkflowOrchestrationSettings(MediaOpsPlanApi planApi, StorageWorkflow.ConfigurationInstance instance) : base(instance.ID.Id)
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

        internal StorageWorkflow.ConfigurationInstance OriginalInstance => originalInstance;

        public override OrchestrationSettings AddCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            capabilitySettings.Add(new WorkflowCapabilitySetting(capabilitySetting));
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
                numberCapacitySettings.Add(new WorkflowNumberCapacitySetting(numberCapacity));
            }
            else if (capacitySetting is RangeCapacitySetting rangeCapacity)
            {
                rangeCapacitySettings.Add(new WorkflowRangeCapacitySetting(rangeCapacity));
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
                textConfigurationSettings.Add(new WorkflowTextConfigurationSetting(textConfiguration));
            }
            else if (configurationSetting is NumberConfigurationSetting numberConfiguration)
            {
                numberConfigurationSettings.Add(new WorkflowNumberConfigurationSetting(numberConfiguration));
            }
            else if (configurationSetting is DiscreteTextConfigurationSetting discreteTextConfiguration)
            {
                discreteTextConfigurationSettings.Add(new WorkflowDiscreteTextConfigurationSetting(discreteTextConfiguration));
            }
            else if (configurationSetting is DiscreteNumberConfigurationSetting discreteNumberConfiguration)
            {
                discreteNumberConfigurationSettings.Add(new WorkflowDiscreteNumberConfigurationSetting(discreteNumberConfiguration));
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

            orchestrationEvents.Add(new WorkflowOrchestrationEvent(orchestrationEvent));
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

        internal StorageWorkflow.ConfigurationInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageWorkflow.ConfigurationInstance(Id) : originalInstance.Clone();
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

        private void ParseInstance(MediaOpsPlanApi planApi, StorageWorkflow.ConfigurationInstance instance)
        {
            originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            ParseParameterValues(planApi, instance.ProfileParameterValues);
            ParseOrchestrationEvents(planApi, instance.OrchestrationEvents);
        }

        private void ParseOrchestrationEvents(MediaOpsPlanApi planApi, IList<StorageWorkflow.OrchestrationEventsSection> orchestrationEvents)
        {
            if (orchestrationEvents == null || orchestrationEvents.Count == 0)
            {
                return;
            }

            foreach (var orchestrationEvent in orchestrationEvents)
            {
                this.orchestrationEvents.Add(new WorkflowOrchestrationEvent(planApi, orchestrationEvent));
            }
        }

        private void ParseParameterValues(MediaOpsPlanApi planApi, IList<StorageWorkflow.ProfileParameterValuesSection> parameterValues)
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
                    planApi.Logger.LogInformation(this, $"WorkflowOrchestrationSettings > ParseParameterValues > Profile parameter with ID '{section.ProfileParameterId}' not found.");
                    continue;
                }

                if (profileParameter.IsCapability())
                {
                    capabilitySettings.Add(new WorkflowCapabilitySetting(section));
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

        private void ParseParameterValues_Capacity(Net.Profiles.Parameter profileParameter, StorageWorkflow.ProfileParameterValuesSection section)
        {
            if (profileParameter.IsRange())
            {
                rangeCapacitySettings.Add(new WorkflowRangeCapacitySetting(section));
            }
            else
            {
                numberCapacitySettings.Add(new WorkflowNumberCapacitySetting(section));
            }
        }

        private void ParseParameterValues_Configuration(Net.Profiles.Parameter profileParameter, StorageWorkflow.ProfileParameterValuesSection section)
        {
            if (profileParameter.IsText())
            {
                textConfigurationSettings.Add(new WorkflowTextConfigurationSetting(section));
            }
            else if (profileParameter.IsNumber())
            {
                numberConfigurationSettings.Add(new WorkflowNumberConfigurationSetting(section));
            }
            else if (profileParameter.IsTextDiscreet())
            {
                discreteTextConfigurationSettings.Add(new WorkflowDiscreteTextConfigurationSetting(new DiscreteTextConfiguration(profileParameter), section));
            }
            else if (profileParameter.IsNumberDiscreet())
            {
                discreteNumberConfigurationSettings.Add(new WorkflowDiscreteNumberConfigurationSetting(new DiscreteNumberConfiguration(profileParameter), section));
            }
        }
    }
}
