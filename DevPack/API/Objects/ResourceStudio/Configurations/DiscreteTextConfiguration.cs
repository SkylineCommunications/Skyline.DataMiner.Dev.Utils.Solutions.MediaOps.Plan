namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for discrete text settings, providing functionality to manage and parse text-based
    /// configurations.
    /// </summary>
    public class DiscreteTextConfiguration : Configuration
    {
        private readonly Dictionary<string, string> discretes = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class.
        /// </summary>
        public DiscreteTextConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration instance.</param>
        public DiscreteTextConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class using the specified
        /// parameter.
        /// </summary>
        /// <param name="parameter">The parameter used to configure the discrete text settings.</param>
        internal DiscreteTextConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
        {
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the display key of the default discrete value.
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// Gets a read-only dictionary of discrete values.
        /// </summary>
        public IReadOnlyDictionary<string, string> Discretes => discretes;

        /// <summary>
        /// Adds a new discrete value with the specified display name and associated value to the configuration.
        /// </summary>
        /// <param name="displayValue">The display name for the discrete value to add. Cannot be null. Must be unique within the configuration.</param>
        /// <param name="value">The value associated with the discrete display name.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="displayValue"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if a discrete with the specified <paramref name="displayValue"/> already exists in the configuration.</exception>
        public DiscreteTextConfiguration AddDiscrete(string displayValue, string value)
        {
            if (displayValue == null)
                throw new ArgumentNullException(nameof(displayValue));

            if (discretes.ContainsKey(displayValue))
                throw new ArgumentException($"The configuration already defines a discreet with display value '{displayValue}'");

            discretes.Add(displayValue, value);
            return this;
        }

        /// <summary>
        /// Removes the discrete value with the specified display value from the collection.
        /// </summary>
        /// <remarks>If the specified display value is set as the default value, removing it will also
        /// clear the default. This method has no effect if the value does not exist in the collection.</remarks>
        /// <param name="displayValue">The display value of the discrete item to remove. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="displayValue"/> is null.</exception>
        public DiscreteTextConfiguration RemoveDiscrete(string displayValue)
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
        /// <param name="discretes">A read-only dictionary containing the discrete keys and their corresponding values to set. Each key-value
        /// pair will be added or updated.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="discretes"/> is <see langword="null"/>.</exception>
        public DiscreteTextConfiguration SetDiscretes(IReadOnlyDictionary<string, string> discretes)
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
                    hash = (hash * 23) + (discreet.Value != null ? discreet.Value.GetHashCode() : 0);
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not DiscreteTextConfiguration other)
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
                if (!other.discretes.TryGetValue(kvp.Key, out var otherValue))
                {
                    return false;
                }
                if (!String.Equals(kvp.Value, otherValue))
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
                discretes.Add(parameter.DiscreetDisplayValues[i], parameter.Discretes[i]);
            }
        }
    }
}
