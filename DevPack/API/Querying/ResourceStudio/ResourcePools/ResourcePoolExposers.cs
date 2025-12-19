namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="ResourcePool"/> objects.
    /// </summary>
    public class ResourcePoolExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.Id"/> property.
        /// </summary>
        public static readonly Exposer<ResourcePool, Guid> Id = new Exposer<ResourcePool, Guid>((obj) => obj.Id, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="ResourcePool.Name"/> property.
        /// </summary>
        public static readonly Exposer<ResourcePool, string> Name = new Exposer<ResourcePool, string>((obj) => obj.Name, "Name");

        /// <summary>
        /// Gets an exposer for the <see cref="ResourcePool.State"/> property.
        /// </summary>
        public static readonly Exposer<ResourcePool, int> State = new Exposer<ResourcePool, int>((obj) => (int)obj.State, "State");

        /// <summary>
        /// Gets an exposer for the <see cref="ResourcePool.IconImage"/> property.
        /// </summary>
        public static readonly Exposer<ResourcePool, string> IconImage = new Exposer<ResourcePool, string>((obj) => obj.IconImage, "IconImage");

        /// <summary>
        /// Gets an exposer for the <see cref="ResourcePool.Url"/> property.
        /// </summary>
        public static readonly Exposer<ResourcePool, string> Url = new Exposer<ResourcePool, string>((obj) => obj.Url, "Url");

        /// <summary>
        /// Provides exposers for querying and filtering linked resource pools.
        /// </summary>
        public static partial class LinkedResourcePools
        {
            /// <summary>
            /// Gets a dynamic list exposer for linked resource pool IDs.
            /// </summary>
            public static readonly DynamicListExposer<ResourcePool, Guid> LinkedResourcePoolId = DynamicListExposer<ResourcePool, Guid>.CreateFromListExposer(new Exposer<ResourcePool, IEnumerable>((obj) => obj.LinkedResourcePools.Where(x => x != null).Select(x => x.LinkedResourcePoolId).Where(x => x != null), "LinkedResourcePools.LinkedResourcePoolId"));

            /// <summary>
            /// Gets a dynamic list exposer for resource selection types.
            /// </summary>
            public static readonly DynamicListExposer<ResourcePool, ResourceSelectionType> SelectionType = DynamicListExposer<ResourcePool, ResourceSelectionType>.CreateFromListExposer(new Exposer<ResourcePool, IEnumerable>((obj) => obj.LinkedResourcePools.Where(x => x != null).Select(x => x.SelectionType), "LinkedResourcePools.SelectionType"));
        }

        /// <summary>
        /// Provides exposers for querying and filtering resource pool capabilities.
        /// </summary>
        public static partial class Capabilities
        {
            /// <summary>
            /// Gets a dynamic list exposer for capability IDs.
            /// </summary>
            public static readonly DynamicListExposer<ResourcePool, Guid> CapabilityId = DynamicListExposer<ResourcePool, Guid>.CreateFromListExposer(new Exposer<ResourcePool, IEnumerable>((obj) => obj.Capabilities.Where(x => x != null).Select(x => x.Id).Where(x => x != null), "Capabilities.Id"));

            /// <summary>
            /// Gets a dynamic list exposer for capability discrete values.
            /// </summary>
            public static readonly DynamicListExposer<ResourcePool, string> Discretes = DynamicListExposer<ResourcePool, string>.CreateFromListExposer(new Exposer<ResourcePool, IEnumerable>((obj) => obj.Capabilities.Where(x => x != null).Select(x => x.Discretes).Where(x => x != null), "Capabilities.Discretes"));
        }
    }
}
