namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;

    using Skyline.DataMiner.Net;

    internal class DomHelpers
    {
        private readonly Lazy<SlcResourceStudioHelper> lazySlcResourceStudioHelper;

        public DomHelpers(IConnection connection)
        {
            lazySlcResourceStudioHelper = new Lazy<SlcResourceStudioHelper>(() => new SlcResourceStudioHelper(connection));
        }

        public SlcResourceStudioHelper SlcResourceStudioHelper => lazySlcResourceStudioHelper.Value;
    }
}
