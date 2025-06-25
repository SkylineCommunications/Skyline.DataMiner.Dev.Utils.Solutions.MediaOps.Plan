namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Skyline.DataMiner.Net;

    internal class CoreHelpers
    {
        private readonly IConnection connection;

        public CoreHelpers(IConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
