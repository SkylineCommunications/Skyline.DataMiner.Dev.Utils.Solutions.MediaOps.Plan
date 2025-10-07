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

        protected internal override void InternalParseParameter(Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
