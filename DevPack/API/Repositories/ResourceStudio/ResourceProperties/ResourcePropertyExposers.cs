namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="ResourceProperty"/> objects.
    /// </summary>
    public class ResourcePropertyExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="ResourceProperty.Id"/> property.
        /// </summary>
        public static readonly Exposer<ResourceProperty, Guid> Id = new Exposer<ResourceProperty, Guid>((obj) => obj.Id, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="ResourceProperty.Name"/> property.
        /// </summary>
        public static readonly Exposer<ResourceProperty, string> Name = new Exposer<ResourceProperty, string>((obj) => obj.Name, "Name");
    }
}
