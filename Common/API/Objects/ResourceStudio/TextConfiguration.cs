namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public class TextConfiguration : Configuration
    {
        public TextConfiguration() : base()
        {
        }

        public TextConfiguration(Guid id) : base(id)
        {
        }

        internal TextConfiguration(Net.Profiles.Parameter profile) : base(profile)
        {
        }

        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
