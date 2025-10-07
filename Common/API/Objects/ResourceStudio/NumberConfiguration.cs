namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;

    public class NumberConfiguration : Configuration
    {
        public NumberConfiguration() : base()
        {
        }

        public NumberConfiguration(Guid id) : base(id)
        {
        }

        protected internal override void InternalParseParameter(Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
