namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    //
    // Summary:
    //     Represents a definition of a capability with an ID and name.
    internal class CoreCapabilityDefinition
    {
        //
        // Summary:
        //     Gets the unique identifier for the capability.
        public Guid Id { get; }

        //
        // Summary:
        //     Gets the name of the capability.
        public string Name { get; }

        //
        // Summary:
        //     Gets or sets a value indicating whether the capability is for internal use only.
        public bool InternalUse { get; internal set; }

        //
        // Summary:
        //     Initializes a new instance of the Skyline.DataMiner.Utils.MediaOps.Common.Core.Components.CapabilityDefinition
        //     class.
        //
        // Parameters:
        //   id:
        //     The unique identifier for the capability.
        //
        //   name:
        //     The name of the capability.
        internal CoreCapabilityDefinition(Guid id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
