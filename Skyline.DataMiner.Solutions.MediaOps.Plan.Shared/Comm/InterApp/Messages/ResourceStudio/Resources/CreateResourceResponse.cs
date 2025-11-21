namespace Skyline.DataMiner.Solutions.MediaOps.Plan
{
    using System;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;

    internal class CreateResourceResponse : Message
    {
        public Guid ResourceId { get; set; }
    }
}
