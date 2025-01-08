namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    /// <summary>
    /// Represents the configuration for an unmanaged resource.
    /// </summary>
    public class ResourceConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the desired status of the resource.
        /// </summary>
        public DesiredStatus DesiredStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the resource is a favorite.
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets the concurrency for the resource.
        /// </summary>
        public long Concurrency { get; set; }

        /// <summary>
        /// Gets or sets the MediaOps input ID of the virtual signal group.
        /// </summary>
        public Guid VirtualSignalGroupInputId { get; set; }

        /// <summary>
        /// Gets or sets the MediaOps output ID of the virtual signal group.
        /// </summary>
        public Guid VirtualSignalGroupOutputId { get; set; }

        /// <summary>
        /// Gets or sets the MediaOps cost rate card ID associated with the resource.
        /// </summary>
        public Guid CostRatecardId { get; set; }

        /// <summary>
        /// Gets or sets the icon image of the resource.
        /// </summary>
        public string IconImage { get; set; }

        /// <summary>
        /// Gets or sets the URL of the resource.
        /// </summary>
        public string URL { get; set; }
    }
}
