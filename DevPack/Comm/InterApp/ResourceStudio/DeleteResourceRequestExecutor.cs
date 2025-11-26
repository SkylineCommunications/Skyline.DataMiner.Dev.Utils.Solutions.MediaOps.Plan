namespace Skyline.DataMiner.Solutions.MediaOps.Plan.DevPack.InterApp
{
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages;

    internal class DeleteResourceRequestExecutor : MediaOpsMessageExecutor<DeleteResourceRequest>
    {
        public DeleteResourceRequestExecutor(DeleteResourceRequest message) : base(message)
        {
        }

        public override Message Execute(MediaOpsPlanApi api)
        {
            api.Resources.Delete(Message.ResourceIds);
            return new DeleteResourceResponse();
        }
    }
}
