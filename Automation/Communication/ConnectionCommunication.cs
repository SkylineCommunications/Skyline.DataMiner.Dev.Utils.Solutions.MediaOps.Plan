namespace Skyline.DataMiner.MediaOps.API.Automation
{
    using System;

    using Skyline.DataMiner.MediaOps.API.Common;
    using Skyline.DataMiner.Net;

    internal class ConnectionCommunication : ICommunication
    {
        internal ConnectionCommunication(IConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public IConnection Connection { get; }
    }
}
