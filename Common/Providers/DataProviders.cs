namespace Skyline.DataMiner.MediaOps.API.Common.Providers
{
    using System;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.Storage.DOM.ResourceStudio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;

    internal class DataProviders
    {
        private readonly ICommunication communication;

        private Lazy<ResourceStudioProvider> lazyResourceStudioProvider;

        private Lazy<ServiceManagerHelper> lazyServiceManagerHelper;
        private Lazy<ResourceManagerHelper> lazyResourceManagerHelper;
        private Lazy<ProtocolFunctionHelper> lazyProtocolFunctionHelper;
        private Lazy<ProfileHelper> lazyProfileHelper;

        internal DataProviders(ICommunication communication)
        {
            this.communication = communication ?? throw new ArgumentNullException(nameof(communication));

            Init();
        }

        public ResourceStudioProvider ResourceStudioProvider => lazyResourceStudioProvider.Value;

        public ServiceManagerHelper ServiceManagerHelper => lazyServiceManagerHelper.Value;

        public ResourceManagerHelper ResourceManagerHelper => lazyResourceManagerHelper.Value;

        public ProtocolFunctionHelper ProtocolFunctionHelper => lazyProtocolFunctionHelper.Value;

        public ProfileHelper ProfileHelper => lazyProfileHelper.Value;

        private void Init()
        {
            lazyResourceStudioProvider = new Lazy<ResourceStudioProvider>(() => new ResourceStudioProvider(new DomHelper(communication.Connection.HandleMessages, SlcResource_StudioIds.ModuleId)));

            lazyServiceManagerHelper = new Lazy<ServiceManagerHelper>(() =>
            {
                var helper = new ServiceManagerHelper();
                helper.RequestResponseEvent += (sender, e) => e.responseMessage = communication.Connection.HandleSingleResponseMessage(e.requestMessage);

                return helper;
            });
            lazyResourceManagerHelper = new Lazy<ResourceManagerHelper>(() => new ResourceManagerHelper(communication.Connection.HandleSingleResponseMessage));
            lazyProtocolFunctionHelper = new Lazy<ProtocolFunctionHelper>(() => new ProtocolFunctionHelper(communication.Connection.HandleMessages));
            lazyProfileHelper = new Lazy<ProfileHelper>(() => new ProfileHelper(communication.Connection.HandleMessages));
        }
    }
}
