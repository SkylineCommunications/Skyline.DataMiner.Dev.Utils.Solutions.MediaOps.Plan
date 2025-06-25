namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using Skyline.DataMiner.Net;

    internal class SlcResourceStudioHelper : DomModuleHelperBase
    {
        public SlcResourceStudioHelper(IConnection connection) : base(SlcResource_Studio.SlcResource_StudioIds.ModuleId, connection)
        {
        }
    }
}
