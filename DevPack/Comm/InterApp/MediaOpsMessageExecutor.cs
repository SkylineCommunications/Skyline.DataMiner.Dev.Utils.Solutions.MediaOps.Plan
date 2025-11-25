namespace Skyline.DataMiner.Solutions.MediaOps.Plan.DevPack.InterApp
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Core.InterAppCalls.Common.MessageExecution;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages;

    internal abstract class MediaOpsMessageExecutor<T> : SimpleMessageExecutor<T> where T : Message
    {
        protected MediaOpsMessageExecutor(T message) : base(message)
        {
        }

        public override bool TryExecute(object dataSource, object dataDestination, out Message optionalReturnMessage)
        {
            var api = dataDestination as MediaOpsPlanApi ?? throw new ArgumentException("Data Destination is not of type MediaOpsPlanApi", nameof(dataDestination));

            try
            {
                optionalReturnMessage = Execute(api);
                optionalReturnMessage.Guid = Message.Guid;
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

        public abstract Message Execute(MediaOpsPlanApi api);
    }
}
