namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.Net.Profiles;

    public class DiscreteNumberConfiguration : Configuration
    {
        public DiscreteNumberConfiguration() : base()
        {
        }

        public DiscreteNumberConfiguration(Guid id) : base(id)
        {
        }

        protected internal override void InternalParseParameter(Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
