namespace Skyline.DataMiner.MediaOps.API.Common.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;
    using Skyline.DataMiner.MediaOps.API.Common.API.People;
    using Skyline.DataMiner.MediaOps.API.Common.API.Teams;
    using Skyline.DataMiner.MediaOps.API.Common.ResourceStudio;

    public interface IMediaOps
    {
        IApiCollection<IPerson, PersonConfig> People { get; }

        IApiCollection<ITeam, TeamConfig> Teams { get; }

        IApiCollection<IOrganization, OrganizationConfig> Organizations { get; }
    }
}
