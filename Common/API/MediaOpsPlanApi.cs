namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallSingle;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages;

    /// <summary>
    /// Provides the main entry point for interacting with the MediaOps Plan API.
    /// </summary>
    public partial class MediaOpsPlanApi
    {
        private const string MediaOpsProtocolName = "MediaOps Plan Manager";
        private const int InterAppReceive_ParameterId = 9000000;
        private const int InterApp_Timeout_ParameterId = 100;

        private readonly string assemblyVersion = typeof(MediaOpsPlanApi).Assembly.GetName().Version.ToString();

        private IDmsElement mediaOpsElement;
        private string mediaOpsElementVersion;
        private bool isCompatible;
        private TimeSpan timeout;

        internal void Init()
        {
            UpdateMediaOpsElement();
        }

        public bool UpdateMediaOpsElement()
        {
            var element = Dms.GetElements().Where(x => x.Protocol.Name.Equals(MediaOpsProtocolName, StringComparison.OrdinalIgnoreCase));
            if (!element.Any())
            {
                return false;
            }

            if (element.Count() > 1)
            {
                Logger.LogWarning("Multiple MediaOps Plan Manager elements found. Using the first one.");
            }

            mediaOpsElement = element.First();
            mediaOpsElementVersion = mediaOpsElement.Protocol.Version;
            if (mediaOpsElementVersion == "production")
            {
                mediaOpsElementVersion = mediaOpsElement.Protocol.ReferencedVersion;
            }

            var splitAssemblyVersion = assemblyVersion.Split('.');
            var splitElementVersion = mediaOpsElementVersion.Split('.');

            isCompatible = splitAssemblyVersion.Length >= 2 && splitElementVersion.Length >= 2 &&
                           splitAssemblyVersion[0] == splitElementVersion[0] &&
                           splitAssemblyVersion[1] == splitElementVersion[1];

            UpdateTimeout();

            return true;
        }

        public void UpdateTimeout()
        {
            try
            {
                var timeoutInSeconds = mediaOpsElement.GetStandaloneParameter<double?>(InterApp_Timeout_ParameterId) ?? throw new NullReferenceException("InterApp Timeout value is null.");
                timeout = TimeSpan.FromSeconds(timeoutInSeconds.GetValue().Value);
                Logger.LogInformation($"Updated Timeout timespan: {timeout}");
            }
            catch (Exception e)
            {
                timeout = TimeSpan.FromSeconds(30);
                Logger.LogInformation($"Unable to retrieve timeout due to: {e}");
            }
        }

        public bool IsSolutionInstalled => mediaOpsElement != null;

        public bool IsSolutionCompatible => isCompatible;

        internal void ThrownIfNotCompatible()
        {
            if (!IsSolutionCompatible)
            {
                throw new InvalidOperationException($"MediaOps Plan API version {assemblyVersion} is not compatible with installed MediaOps Plan version {mediaOpsElementVersion}.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Type of the expected response.</typeparam>
        /// <param name="message">Message to be sent.</param>
        /// <returns>Response of expected type.</returns>
        /// <exception cref="MediaOpsException">If the response indicates a failure.</exception>
        /// <exception cref="InvalidCastException">If response could not be cast to the provided type.</exception>
        internal T SendMessage<T>(Message message) where T : Message
        {
            var commands = InterAppCallFactory.CreateNew();
            commands.Messages.Add(message);

            Logger.LogInformation($"Sending InterApp Message: {JsonConvert.SerializeObject(message)}");

            var response = commands.Send(connection, mediaOpsElement.AgentId, mediaOpsElement.Id, InterAppReceive_ParameterId, timeout, knownTypes).First();

            Logger.LogInformation($"InterApp Response: {JsonConvert.SerializeObject(response)}");

            if (response is OperationFailedResponse operationFailedResponse)
            {
                throw new MediaOpsException(operationFailedResponse.TraceData);
            }
            else if (response is T castResponse)
            {
                return castResponse;
            }
            else
            {
                throw new InvalidCastException($"Received response is not of type {typeof(T)}");
            }
        }

        /// <summary>
        /// Sends an InterApp request without expecting a response.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        internal void SendMessage(Message message)
        {
            var commands = InterAppCallFactory.CreateNew();
            commands.Messages.Add(message);

            Logger.LogInformation($"Sending InterApp Message: {JsonConvert.SerializeObject(message)}");

            commands.Send(connection, mediaOpsElement.AgentId, mediaOpsElement.Id, InterAppReceive_ParameterId, knownTypes);
        }
    }
}
