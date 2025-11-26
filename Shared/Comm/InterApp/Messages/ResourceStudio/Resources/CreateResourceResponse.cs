namespace Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages
{
    using System;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    internal class CreateResourceResponse : Message
    {
        public Guid[] ResourceIds { get; set; }
    }
}
