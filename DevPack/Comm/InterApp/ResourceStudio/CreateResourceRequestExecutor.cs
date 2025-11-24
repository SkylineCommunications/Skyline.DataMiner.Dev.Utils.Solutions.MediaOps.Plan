namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Core.InterAppCalls.Common.MessageExecution;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages;

    internal class CreateResourceRequestExecutor : SimpleMessageExecutor<CreateResourceRequest>
    {
        public CreateResourceRequestExecutor(CreateResourceRequest message) : base(message)
        {
        }

        public override bool TryExecute(object dataSource, object dataDestination, out Message optionalReturnMessage)
        {
            var api = dataDestination as MediaOpsPlanApi ?? throw new ArgumentException("Data Destination is not of type MediaOpsPlanApi", nameof(dataDestination));

            try
            {
                var resourceId = api.Resources.Create(Message.Resource);
                optionalReturnMessage = new CreateResourceResponse
                {
                    Guid = Message.Guid,
                    ResourceId = resourceId,
                };
            }
            catch (Exception ex)
            {
                api.Logger.LogError(ex, "Exception occurred: {exception}", ex.ToString());

                optionalReturnMessage = new OperationFailedResponse
                {
                    Guid = Message.Guid,
                    Exception = ex,
                };
            }

            return true;
        }
    }
}
