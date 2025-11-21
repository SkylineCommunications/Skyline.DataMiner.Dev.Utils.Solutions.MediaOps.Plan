namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages
{
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal class OperationFailedResponse : Message
    {
        public MediaOpsTraceData TraceData { get; set; }
    }
}
