namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections;
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
    public class DiscreteTextConfigurationExposers : ConfigurationExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="DiscreteTextConfiguration.DefaultValue"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, string> DefaultValue = new SettableExposer<Configuration, string>((Func<Configuration, string>)((Configuration x) => (!(x is DiscreteTextConfiguration discreteTextConfiguration)) ? null : discreteTextConfiguration.DefaultValue), (Action<Configuration, string>)delegate
        {
        }, new string[1] { "DefaultValue" });

        /// <summary>
        /// Gets a dynamic list exposer for the <see cref="DiscreteTextConfiguration.Discretes"/> property.
        /// </summary>
        public static readonly DynamicListExposer<Configuration, string> Discretes = DynamicListExposer<Configuration, string>.CreateFromListExposer(new Exposer<Configuration, IEnumerable>(x => (x is DiscreteTextConfiguration discreteTextConfiguration) ? discreteTextConfiguration.Discretes.Values : new string[0], "Discretes"));
    }

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="DiscreteNumberConfiguration"/> objects.
    /// </summary>
    public class DiscreteNumberConfigurationExposers : ConfigurationExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="DiscreteNumberConfiguration.DefaultValue"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, string> DefaultValue = new SettableExposer<Configuration, string>((Func<Configuration, string>)((Configuration x) => (!(x is DiscreteNumberConfiguration discreteNumberConfiguration)) ? null : discreteNumberConfiguration.DefaultValue), (Action<Configuration, string>)delegate
        {
        }, new string[1] { "DefaultValue" });

        /// <summary>
        /// Gets a dynamic list exposer for the <see cref="DiscreteNumberConfiguration.Discretes"/> property.
        /// </summary>
        public static readonly DynamicListExposer<Configuration, decimal> Discretes = DynamicListExposer<Configuration, decimal>.CreateFromListExposer(new Exposer<Configuration, IEnumerable>(x => (x is DiscreteNumberConfiguration discreteNumberConfiguration) ? discreteNumberConfiguration.Discretes.Values : new decimal[0], "Discretes"));
    }

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="NumberConfiguration"/> objects.
    /// </summary>
    public class NumberConfigurationExposers : ConfigurationExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="NumberConfiguration.DefaultValue"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, decimal> DefaultValue = new SettableExposer<Configuration, decimal>((Func<Configuration, decimal>)((Configuration x) => (x is NumberConfiguration numberConfiguration && numberConfiguration.DefaultValue.HasValue) ? numberConfiguration.DefaultValue.Value : -1m), (Action<Configuration, decimal>)delegate
        {
        }, new string[1] { "DefaultValue" });
    }

    /// <summary>
    /// Provides exposers for querying and filtering <see cref="TextConfiguration"/> objects.
    /// </summary>
    public class TextConfigurationExposers : ConfigurationExposers
    {
        /// <summary>
        /// Gets an exposer for the <see cref="TextConfiguration.DefaultValue"/> property.
        /// </summary>
        public static readonly Exposer<Configuration, string> DefaultValue = new SettableExposer<Configuration, string>((Func<Configuration, string>)((Configuration x) => (!(x is TextConfiguration textConfiguration)) ? null : textConfiguration.DefaultValue), (Action<Configuration, string>)delegate
        {
        }, new string[1] { "DefaultValue" });
    }
}
