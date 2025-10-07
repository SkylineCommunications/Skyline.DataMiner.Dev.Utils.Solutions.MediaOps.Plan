namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public class NumberConfiguration : Configuration
    {
        public NumberConfiguration() : base()
        {
        }

        public NumberConfiguration(Guid id) : base(id)
        {
        }

        internal NumberConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
        {
        }

        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
