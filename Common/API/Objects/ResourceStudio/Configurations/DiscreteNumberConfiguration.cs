namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net;

    /// <summary>
    /// Represents a configuration for discrete numerical values.
    /// </summary>
    public class DiscreteNumberConfiguration : Configuration
    {
        private string defaultValue;
        private Dictionary<string, decimal> discretes = new Dictionary<string, decimal>(); // TODO: should we use a dictionary here? This doesn't allow multiple discretes with the same key, which could make it harder when creating UIs. We could always validate when pushing the Configuration.

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class.
        /// </summary>
        public DiscreteNumberConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration instance.</param>
        public DiscreteNumberConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class with the specified
        /// parameter.
        /// </summary>
        /// <param name="parameter">The parameter used to configure the discrete number settings.</param>
        internal DiscreteNumberConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
        {
        }

        public string DefaultValue
        {
            get => defaultValue;
            set
            {
                HasChanges = true;
                defaultValue = value;
            }
        }

        /// <summary>
        /// Gets a read-only dictionary of discrete values.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> Discretes => discretes;

        public void AddDiscrete(string displayValue, decimal value)
        {
            if (displayValue == null)
                throw new ArgumentNullException(nameof(displayValue));

            if (Double.IsNaN((double)value))
                throw new ArgumentException($"{nameof(value)} cannot be NaN");

            if (Double.IsInfinity((double)value))
                throw new ArgumentException($"{nameof(value)} cannot be an infinite");

            if (discretes.ContainsKey(displayValue))
                throw new ArgumentException($"The configuration already defines a discreet with display value '{displayValue}'");

            discretes.Add(displayValue, value);
            HasChanges = true;
        }

        public void RemoveDiscrete(string displayValue)
        {
            if (displayValue == null)
                throw new ArgumentNullException(nameof(displayValue));

            if (!discretes.Remove(displayValue))
            {
                return;
            }

            if (String.Equals(DefaultValue, displayValue))
            {
                DefaultValue = null;
            }

            HasChanges = true;
        }

        public void SetDiscretes(IReadOnlyDictionary<string, decimal> discretes)
        {
            if (discretes == null)
                throw new ArgumentNullException(nameof(discretes));

            foreach (var kvp in discretes)
            {
                AddDiscrete(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            if (!parameter.HasDefaultStringValue())
            {
                DefaultValue = null;
            }
            else
            {
                int defaultIndex = parameter.Discretes.IndexOf(parameter.DefaultValue.StringValue);
                DefaultValue = defaultIndex >= 0 ? parameter.DiscreetDisplayValues[defaultIndex] : null;
            }

            for (int i = 0; i < parameter.Discretes.Count; i++)
            {
                discretes.Add(parameter.DiscreetDisplayValues[i], Decimal.Parse(parameter.Discretes[i]));
            }
        }
    }
}
