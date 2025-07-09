namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;

    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal abstract class DomModuleHelperBase
    {
        private readonly string moduleId;
        private readonly IConnection connection;

        protected DomModuleHelperBase(string moduleId, IConnection connection)
        {
            this.moduleId = moduleId ?? throw new ArgumentNullException(nameof(moduleId));
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            DomHelper = new DomHelper(connection.HandleMessages, moduleId);
        }

        public DomHelper DomHelper { get; }
    }
}
