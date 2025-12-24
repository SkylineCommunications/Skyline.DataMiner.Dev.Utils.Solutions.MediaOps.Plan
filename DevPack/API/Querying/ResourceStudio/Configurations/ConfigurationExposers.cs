namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="Configuration"/> objects.
    /// </summary>
    public class ConfigurationExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="ApiObject.Id"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, Guid> Id = new Exposer<Configuration, Guid>((obj) => obj.Id, "Id");

        /// <summary>
        /// Gets an exposer for the <see cref="Parameter.IsMandatory"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, bool> IsMandatory = new Exposer<Configuration, bool>((obj) => obj.IsMandatory, "IsMandatory");

        /// <summary>
        /// Gets an exposer for the <see cref="Parameter.Name"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, string> Name = new Exposer<Configuration, string>((obj) => obj.Name, "Name");
    }

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="DiscreteTextConfiguration"/> objects.
    /// </summary>
    public class DiscreteTextConfigurationExposers
    {
        /// <summary>
        /// Gets a dynamic list exposer for the <see cref="DiscreteTextConfiguration.Discretes"/> property.
        /// </summary>
        public static readonly DynamicListExposer<Configuration, string> Discretes = DynamicListExposer<Configuration, string>.CreateFromListExposer(new Exposer<Configuration, IEnumerable>(x => (x is DiscreteTextConfiguration discreteTextConfiguration) ? discreteTextConfiguration.Discretes.Values.ToList() : new List<string>(), "DiscreteTextConfiguration.Discretes"));
    }

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="DiscreteNumberConfiguration"/> objects.
    /// </summary>
    public class DiscreteNumberConfigurationExposers
    {
        /// <summary>
        /// Gets a dynamic list exposer for the <see cref="DiscreteNumberConfiguration.Discretes"/> property.
        /// </summary>
        public static readonly DynamicListExposer<Configuration, decimal> Discretes = DynamicListExposer<Configuration, decimal>.CreateFromListExposer(new Exposer<Configuration, IEnumerable>(x => (x is DiscreteNumberConfiguration discreteNumberConfiguration) ? discreteNumberConfiguration.Discretes.Values.ToList() : new List<decimal>(), "DiscreteNumberConfiguration.Discretes"));
    }

    // Unable to provide Exposers for RangeNumberConfiguration and FreeTextConfiguration as they only expose DefaultValues and those cannot be mapped to ParameterExposers.
}
