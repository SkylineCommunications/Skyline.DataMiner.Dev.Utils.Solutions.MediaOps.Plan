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
        /// Gets an exposer for the <see cref="ApiObject.ID"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, Guid> Id = new Exposer<Capacity, Guid>("Id");

        /// <summary>
        /// Gets an exposer for the <see cref="Parameter.IsMandatory"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> IsMandatory = new Exposer<Capacity, bool>((obj) => obj.IsMandatory, "IsMandatory");

        /// <summary>
        /// Gets an exposer for the <see cref="Parameter.Name"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, string> Name = new Exposer<Capacity, string>((obj) => obj.Name, "Name");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Units"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, string> Units = new Exposer<Capacity, string>((obj) => obj.Units, "Units");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Units"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> HasUnits = new Exposer<Capacity, bool>("HasUnits");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.RangeMin"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal> RangeMin = new Exposer<Capacity, decimal>((obj) => (decimal)obj.RangeMin, "RangeMin");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.RangeMin"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> HasRangeMin = new Exposer<Capacity, bool>("HasRangeMin");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.RangeMax"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal> RangeMax = new Exposer<Capacity, decimal>((obj) => (decimal)obj.RangeMax, "RangeMax");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Units"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> HasRangeMax = new Exposer<Capacity, bool>("HasRangeMax");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.StepSize"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, decimal> StepSize = new Exposer<Capacity, decimal>((obj) => (decimal)obj.StepSize, "StepSize");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.StepSize"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> HasStepSize = new Exposer<Capacity, bool>("HasStepSize");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Decimals"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, int> Decimals = new Exposer<Capacity, int>((obj) => (int)obj.Decimals, "Decimals");

        /// <summary>
        /// Gets an exposer for the <see cref="Capacity.Decimals"/> property.
        /// </summary>
        public static readonly Exposer<Capacity, bool> HasDecimals = new Exposer<Capacity, bool>("HasDecimals");
    }
}
