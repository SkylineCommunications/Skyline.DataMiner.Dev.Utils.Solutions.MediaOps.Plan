namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    internal class ConfiguredCapability : IConfiguredCapability
    {
        public ConfiguredCapability(Guid profileParameterId)
        {
            if (profileParameterId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(profileParameterId));
            }

            ProfileParameterId = profileParameterId;
        }

        public Guid ProfileParameterId { get; private set; }

        public string StringValue { get; set; }
    }
}
