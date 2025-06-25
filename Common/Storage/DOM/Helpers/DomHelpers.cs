namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;

    using Skyline.DataMiner.Net;

    internal class DomHelpers
    {
        private readonly IConnection connection;

        private Lazy<SlcResourceStudioHelper> lazySlcResourceStudioHelper;

        public DomHelpers(IConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            lazySlcResourceStudioHelper = new Lazy<SlcResourceStudioHelper>(() => new SlcResourceStudioHelper(connection));
        }

        public SlcResourceStudioHelper SlcResourceStudioHelper => lazySlcResourceStudioHelper.Value;
    }
}
