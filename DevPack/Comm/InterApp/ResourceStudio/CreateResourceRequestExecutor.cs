namespace Skyline.DataMiner.Solutions.MediaOps.Plan.DevPack.InterApp
{
    using System.Linq;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages;

    internal class CreateResourceRequestExecutor : MediaOpsMessageExecutor<CreateResourceRequest>
    {
        public CreateResourceRequestExecutor(CreateResourceRequest message) : base(message)
        {
        }

        public override Message Execute(MediaOpsPlanApi api)
        {
            var resourceIds = api.Resources.Create(Message.Resources);
            return new CreateResourceResponse
            {
                Guid = Message.Guid,
                ResourceIds = resourceIds.ToArray(),
            };
        }
    }
}
