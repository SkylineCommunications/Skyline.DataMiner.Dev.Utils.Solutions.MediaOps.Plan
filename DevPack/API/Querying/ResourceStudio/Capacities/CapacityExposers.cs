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
        /// Gets an exposer for the <see cref="Capacity.RangeMin"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal?> RangeMin = new Exposer<Capacity, decimal?>((obj) => obj.RangeMin, "RangeMin");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.RangeMax"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal?> RangeMax = new Exposer<Capacity, decimal?>((obj) => obj.RangeMax, "RangeMax");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.StepSize"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal?> StepSize = new Exposer<Capacity, decimal?>((obj) => obj.StepSize, "StepSize");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Decimals"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, int?> Decimals = new Exposer<Capacity, int?>((obj) => obj.Decimals, "Decimals");
    }
}
