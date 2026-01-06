namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for discrete numeric values.
    /// </summary>
    public class DiscreteNumberConfiguration : Configuration
    {
        /// <summary>
        /// The backing collection that stores the configured discrete numeric values.
        /// </summary>
        private readonly List<NumberDiscreet> discretes = new List<NumberDiscreet>();

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
        /// Gets or sets the default discrete numeric value to use when no explicit value is provided.
        /// </summary>
        public NumberDiscreet DefaultValue { get; set; }

        /// <summary>
        /// Gets a read-only collection of configured discrete numeric values.
        /// </summary>
        public IReadOnlyCollection<NumberDiscreet> Discretes => discretes;

        /// <summary>
        /// Adds a discrete number configuration to the current collection.
        /// </summary>
        /// <param name="discreet">The discrete number configuration to add. Cannot be <see langword="null"/>.</param>
        /// <returns>
        /// The current <see cref="DiscreteNumberConfiguration"/> instance, enabling fluent configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="discreet"/> is <see langword="null"/>.</exception>
        public DiscreteNumberConfiguration AddDiscrete(NumberDiscreet discreet)
        {
            if (discreet == null)
                throw new ArgumentNullException(nameof(discreet));

            discretes.Add(discreet);
            return this;
        }

        /// <summary>
        /// Removes all occurrences of the specified discrete numeric value from the configuration.
        /// </summary>
        /// <param name="discreet">The discrete numeric value to remove. Cannot be <see langword="null"/>.</param>
        /// <returns>
        /// The current <see cref="DiscreteNumberConfiguration"/> instance, enabling fluent configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="discreet"/> is <see langword="null"/>.</exception>
        public DiscreteNumberConfiguration RemoveDiscrete(NumberDiscreet discreet)
        {
            if (discreet == null)
                throw new ArgumentNullException(nameof(discreet));

            discretes.RemoveAll(x => x.Equals(discreet));

            return this;
        }

        /// <summary>
        /// Replaces the current discrete values with the specified collection.
        /// </summary>
        /// <param name="discretes">
        /// A collection containing the discrete values to set. Cannot be <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The current <see cref="DiscreteNumberConfiguration"/> instance, enabling fluent configuration.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="discretes"/> is <see langword="null"/>.</exception>
        public DiscreteNumberConfiguration SetDiscretes(ICollection<NumberDiscreet> discretes)
        {
            if (discretes == null)
                throw new ArgumentNullException(nameof(discretes));

            this.discretes.Clear();
            this.discretes.AddRange(discretes);

            return this;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);

                foreach (var discreet in discretes.OrderBy(x => x.DisplayName).ToArray())
                {
                    hash = (hash * 23) + discreet.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="DiscreteNumberConfiguration"/> instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>
        /// <see langword="true"/> if the specified object is a <see cref="DiscreteNumberConfiguration"/> and has
        /// the same base configuration, default value, and discrete values (irrespective of order);
        /// otherwise, <see langword="false"/>.
        /// </returns>
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

            if (DefaultValue != other.DefaultValue)
            {
                return false;
            }

            if (!discretes.ScrambledEquals(other.Discretes))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the specified profile parameter and updates this configuration with its discrete numeric values.
        /// </summary>
        /// <param name="parameter">The core profile parameter to parse.</param>
        /// <remarks>
        /// When the parameter does not contain discrete values, the internal collection and the default value
        /// are cleared. If discrete values and display values are present but their counts differ, an
        /// <see cref="InvalidOperationException"/> is thrown.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the number of discrete values does not match the number of display values.
        /// </exception>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            if (parameter.Discretes == null || parameter.DiscreetDisplayValues == null)
            {
                discretes.Clear();
                DefaultValue = null;
                return;
            }

            if (parameter.Discretes.Count != parameter.DiscreetDisplayValues.Count)
            {
                throw new InvalidOperationException($"Profile parameter {parameter.Name} [{parameter.ID}] has an inconsistent number of discrete values ({parameter.Discretes.Count} vs {parameter.DiscreetDisplayValues.Count}).");
            }

            for (int i = 0; i < parameter.Discretes.Count; i++)
            {
                discretes.Add(new NumberDiscreet(Decimal.Parse(parameter.Discretes[i]), parameter.DiscreetDisplayValues[i]));
            }

            if (!parameter.HasDefaultStringValue())
            {
                DefaultValue = null;
            }
            else
            {
                DefaultValue = discretes.FirstOrDefault(x => x.Value.ToString(CultureInfo.InvariantCulture).Equals(parameter.DefaultValue.StringValue));
            }
        }
    }
}
