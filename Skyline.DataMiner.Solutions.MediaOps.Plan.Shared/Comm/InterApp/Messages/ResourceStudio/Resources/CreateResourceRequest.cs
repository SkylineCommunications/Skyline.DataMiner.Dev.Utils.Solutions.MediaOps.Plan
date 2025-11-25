namespace Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages
{
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal class CreateResourceRequest : Message
    {
        public Resource[] Resources { get; set; }
    }
}
