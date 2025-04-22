namespace Skyline.DataMiner.MediaOps.API.Common.Core.Components
{
    using System;

    /// <summary>
    /// Provides a set of capabilities.
    /// </summary>
    public static class Capabilities
    {
        /// <summary>
        /// Gets the capability definition for resource type.
        /// </summary>
        public static CapabilityDefinition ResourceType { get; } = new CapabilityDefinition(new Guid("f3995889-3d50-4972-8c9f-1eac9c663606"), "RST_ResourceType") { InternalUse = true };

        /// <summary>
        /// Gets an array of all capability definitions.
        /// </summary>
        public static CapabilityDefinition[] AllCapabilities { get; } = new[]
        {
            ResourceType,
        };
    }
}