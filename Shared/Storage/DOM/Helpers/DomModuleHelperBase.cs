namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal abstract class DomModuleHelperBase
    {
        protected DomModuleHelperBase(string moduleId, IConnection connection)
        {
            DomHelper = new DomHelper(connection.HandleMessages, moduleId);
        }

        public DomHelper DomHelper { get; }
    }
}
