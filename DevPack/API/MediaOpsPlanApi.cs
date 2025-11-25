namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.DevPack.InterApp;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.InterApp.Messages;

    /// <summary>
    /// Provides the main entry point for interacting with the MediaOps Plan API.
    /// </summary>
    public partial class MediaOpsPlanApi
    {
        private static readonly Dictionary<Type, Type> messageToExecutor = new Dictionary<Type, Type>
        {
            {typeof(CreateResourceRequest), typeof(CreateResourceRequestExecutor)},
            {typeof(DeleteResourceRequest), typeof(DeleteResourceRequestExecutor)},
        };

        public void HandleInterAppRequest(string rawInterAppCall)
        {
            try
            {
                var receivedCall = InterAppCallFactory.CreateFromRaw(rawInterAppCall, knownTypes);

                foreach (var message in receivedCall.Messages)
                {
                    using (logger.BeginScope(new Dictionary<string, object> { { "InterAppMessage.ID", message.Guid } }))
                    {
                        logger.LogDebug("Received InterApp message: {message}.", JsonConvert.SerializeObject(message));

                        if (!message.TryExecute(null, this, messageToExecutor, out var replyMessage))
                        {
                            throw new InvalidOperationException($"Unable to execute incoming message");
                        }

                        if (!message.ExpectsReply)
                        {
                            logger.LogDebug("Message does not expect reply.");
                            continue;
                        }

                        if (replyMessage is null)
                        {
                            throw new InvalidOperationException($"Unable to build reply message");
                        }

                        logger.LogDebug("Sending InterApp reply: {replymessage}.", JsonConvert.SerializeObject(replyMessage));

                        message.Reply(connection, replyMessage, knownTypes);
                    }
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling incoming inter-app message.");
            }
        }

        internal void Init()
        {
            // Initialization logic here
        }
    }
}
