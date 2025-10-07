namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;

    public class TextConfiguration : Configuration
    {
        public TextConfiguration() : base()
        {
        }

        public TextConfiguration(Guid id) : base(id)
        {
        }

        protected internal override void InternalParseParameter(Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
