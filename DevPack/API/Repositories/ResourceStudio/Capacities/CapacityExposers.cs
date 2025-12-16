namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="Capacity"/> objects.
    /// </summary>
    public class CapacityExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Id"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, Guid> Id = new Exposer<Capacity, Guid>((obj) => obj.Id, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.IsMandatory"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> IsMandatory = new Exposer<Capacity, bool>((obj) => obj.IsMandatory, "IsMandatory");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Name"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, string> Name = new Exposer<Capacity, string>((obj) => obj.Name, "Name");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Units"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, string> Units = new Exposer<Capacity, string>((obj) => obj.Units, "Units");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Category"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, Net.Profiles.ProfileParameterCategory> Category = new Exposer<Capacity, Net.Profiles.ProfileParameterCategory>((obj) => obj.Category, "Category");
    }
}
