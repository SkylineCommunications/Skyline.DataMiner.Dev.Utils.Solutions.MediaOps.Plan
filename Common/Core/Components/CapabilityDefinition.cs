namespace Skyline.DataMiner.MediaOps.API.Common.Core.Components
{
    using System;

    /// <summary>
    /// Represents a definition of a capability with an ID and name.
    /// </summary>
    public class CapabilityDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilityDefinition"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the capability.</param>
        /// <param name="name">The name of the capability.</param>
        internal CapabilityDefinition(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <summary>
        /// Gets the unique identifier for the capability.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the name of the capability.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the capability is for internal use only.
        /// </summary>
        public bool InternalUse { get; internal set; }
    }
}
