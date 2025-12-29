namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for discrete numerical values.
    /// </summary>
    public class DiscreteNumberConfiguration : Configuration
    {
        private string defaultValue;
        private readonly Dictionary<string, decimal> discretes = new Dictionary<string, decimal>(); // TODO: should we use a dictionary here? This doesn't allow multiple discretes with the same key, which could make it harder when creating UIs. We could always validate when pushing the Configuration.

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
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the display key of the default discrete value.
        /// </summary>
        public string DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
            }
        }

        /// <summary>
        /// Gets a read-only dictionary of discrete values.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> Discretes => discretes;

        /// <summary>
        /// Adds a discrete value with the specified display label and numeric value to the configuration.
        /// </summary>
        /// <param name="displayValue">The display label that identifies the discrete value. Cannot be null.</param>
        /// <param name="value">The numeric value associated with the discrete. Must not be NaN or infinite.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="displayValue"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is NaN or infinite, or if a discrete with the same display value already
        /// exists.</exception>
        public DiscreteNumberConfiguration AddDiscrete(string displayValue, decimal value)
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

            return this;
        }

        /// <summary>
        /// Removes the discrete value with the specified display value from the collection.
        /// </summary>
        /// <remarks>If the removed value is currently set as the default value, the default value is
        /// cleared. This method has no effect if the specified value does not exist in the collection.</remarks>
        /// <param name="displayValue">The display value of the discrete item to remove. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="displayValue"/> is null.</exception>
        public DiscreteNumberConfiguration RemoveDiscrete(string displayValue)
        {
            if (displayValue == null)
                throw new ArgumentNullException(nameof(displayValue));

            if (!discretes.Remove(displayValue))
            {
                return this;
            }

            if (String.Equals(DefaultValue, displayValue))
            {
                DefaultValue = null;
            }

            return this;
        }

        /// <summary>
        /// Sets multiple discrete values using the specified key-value pairs.
        /// </summary>
        /// <param name="discretes">A read-only dictionary containing the discrete names and their corresponding decimal values to set. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="discretes"/> is null.</exception>
        public DiscreteNumberConfiguration SetDiscretes(IReadOnlyDictionary<string, decimal> discretes)
        {
            if (discretes == null)
                throw new ArgumentNullException(nameof(discretes));

            this.discretes.Clear();
            foreach (var kvp in discretes)
            {
                AddDiscrete(kvp.Key, kvp.Value);
            }

            return this;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);
                foreach (var discreet in discretes.OrderBy(x => x.Key).ToArray())
                {
                    hash = (hash * 23) + (discreet.Key != null ? discreet.Key.GetHashCode() : 0);
                    hash = (hash * 23) + discreet.Value.GetHashCode();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not DiscreteNumberConfiguration other)
            {
                return false;
            }

            if (!base.Equals(other))
            {
                return false;
            }

            if (!String.Equals(DefaultValue, other.DefaultValue))
            {
                return false;
            }

            if (discretes.Count != other.discretes.Count)
            {
                return false;
            }

            foreach (var kvp in discretes)
            {
                if (!other.discretes.TryGetValue(kvp.Key, out decimal otherValue))
                {
                    return false;
                }

                if (kvp.Value != otherValue)
                {
                    return false;
                }
            }

            return true;
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
