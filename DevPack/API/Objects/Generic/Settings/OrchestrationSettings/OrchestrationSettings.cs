namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class OrchestrationSettings : ApiObject
    {
        internal OrchestrationSettings() : base()
        {
            IsNew = true;
        }

        internal OrchestrationSettings(Guid orchestrationSettingId) : base(orchestrationSettingId)
        {
            IsNew = false;
        }

        public override string Name
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public abstract IReadOnlyCollection<CapabilitySetting> Capabilities { get; }

        public abstract IReadOnlyCollection<CapacitySetting> Capacities { get; }

        public abstract IReadOnlyCollection<ConfigurationSetting> Configurations { get; }

        public abstract IReadOnlyCollection<OrchestrationEvent> OrchestrationEvents { get; }

        public abstract OrchestrationSettings AddCapability(CapabilitySetting capabilitySetting);

        public abstract OrchestrationSettings RemoveCapability(CapabilitySetting capabilitySetting);

        public abstract OrchestrationSettings AddCapacity(CapacitySetting capacitySetting);

        public abstract OrchestrationSettings RemoveCapacity(CapacitySetting capacitySetting);

        public abstract OrchestrationSettings AddConfiguration(ConfigurationSetting configurationSetting);

        public abstract OrchestrationSettings RemoveConfiguration(ConfigurationSetting configurationSetting);

        public abstract OrchestrationSettings AddOrchestrationEvent(OrchestrationEvent orchestrationEvent);

        public abstract OrchestrationSettings RemoveOrchestrationEvent(OrchestrationEvent orchestrationEvent);
    }

    public class OrchestrationEvent : TrackableObject
    {
        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
    }
}
