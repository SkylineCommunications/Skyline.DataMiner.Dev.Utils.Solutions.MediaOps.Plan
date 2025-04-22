namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    /// <summary>
    /// Represents metadata for an object created by the client application.
    /// </summary>
    public class ObjectMetadata
    {
        /// <summary>
        /// The linked DOM Instance ID from the object in the client application.
        /// </summary>
        public Guid DomInstanceId { get; set; }

        /// <summary>
        /// The name of the automation script that will be executed when a Resource Studio object goes to Error state.
        /// </summary>
        public string ErrorScriptName { get; set; }

        /// <summary>
        /// The name of the automation script that will be executed when a Resource Studio object is used in a job reservation.
        /// </summary>
        public string BookingExtensionScriptName { get; set; }
    }
}