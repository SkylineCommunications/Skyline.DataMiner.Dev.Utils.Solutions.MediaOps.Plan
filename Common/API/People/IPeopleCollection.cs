namespace Skyline.DataMiner.MediaOps.API.Common.API.People
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    public interface IPeopleCollection : IApiQuerable<IPerson>, IApiCreate<CreatePersonRequest>
    {
    }
}
