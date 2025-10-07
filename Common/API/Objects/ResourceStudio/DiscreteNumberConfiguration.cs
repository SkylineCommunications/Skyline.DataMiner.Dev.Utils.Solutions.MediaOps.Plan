namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public class DiscreteNumberConfiguration : Configuration
    {
        public DiscreteNumberConfiguration() : base()
        {
        }

        public DiscreteNumberConfiguration(Guid id) : base(id)
        {
        }

        internal DiscreteNumberConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
        {
        }

        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
