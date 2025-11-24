namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages
{
    using System;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    internal class OperationFailedResponse : Message
    {
        public Exception Exception { get; set; }
    }
}
