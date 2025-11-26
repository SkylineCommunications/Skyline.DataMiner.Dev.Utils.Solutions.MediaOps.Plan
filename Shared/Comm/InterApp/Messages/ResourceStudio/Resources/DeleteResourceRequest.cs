namespace Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages
{
    using System;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    internal class DeleteResourceRequest : Message
    {
        public Guid[] ResourceIds { get; set; }
    }
}
