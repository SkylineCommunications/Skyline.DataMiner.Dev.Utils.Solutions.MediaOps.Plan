namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;

    public class DiscreteTextConfiguration : Configuration
    {
        public DiscreteTextConfiguration() : base()
        {
        }

        public DiscreteTextConfiguration(Guid id) : base(id)
        {
        }

        internal DiscreteTextConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
        {
        }

        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
