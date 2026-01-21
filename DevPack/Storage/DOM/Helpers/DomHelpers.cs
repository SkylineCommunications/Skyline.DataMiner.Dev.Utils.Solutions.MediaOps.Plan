namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;

    using Skyline.DataMiner.Net;

    internal class DomHelpers
    {
        private readonly Lazy<SlcResourceStudioHelper> lazySlcResourceStudioHelper;
        private readonly Lazy<SlcWorkflowHelper> lazySlcWorkflowHelper;

        public DomHelpers(IConnection connection)
        {
            lazySlcResourceStudioHelper = new Lazy<SlcResourceStudioHelper>(() => new SlcResourceStudioHelper(connection));
            lazySlcWorkflowHelper = new Lazy<SlcWorkflowHelper>(() => new SlcWorkflowHelper(connection));
        }

        public SlcResourceStudioHelper SlcResourceStudioHelper => lazySlcResourceStudioHelper.Value;

        public SlcWorkflowHelper SlcWorkflowHelper => lazySlcWorkflowHelper.Value;
    }
}
