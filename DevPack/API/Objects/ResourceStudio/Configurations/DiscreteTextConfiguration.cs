namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	/// <summary>
	/// Represents a configuration for discrete text settings, providing functionality to manage and parse text-based
	/// configurations.
	/// </summary>
	public class DiscreteTextConfiguration : Configuration
	{
		private readonly List<TextDiscreet> discretes = new List<TextDiscreet>();

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
		/// Gets or sets the default value to use when no explicit value is provided.
		/// </summary>
		public TextDiscreet DefaultValue { get; set; }

		/// <summary>
		/// Gets a read-only collection of discrete text values.
		/// </summary>
		public IReadOnlyCollection<TextDiscreet> Discretes => discretes;

		/// <summary>
		/// Adds the specified discrete text configuration to the current collection.
		/// </summary>
		/// <param name="discreet">The discrete text configuration to add. Cannot be null.</param>
		/// <returns>
		/// The current <see cref="DiscreteTextConfiguration"/> instance with the added discrete text configuration,
		/// enabling fluent configuration.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="discreet"/> is null.</exception>
		public DiscreteTextConfiguration AddDiscrete(TextDiscreet discreet)
		{
			if (discreet == null)
				throw new ArgumentNullException(nameof(discreet));

			discretes.Add(discreet);
			return this;
		}

		/// <summary>
		/// Removes the specified discrete value from the configuration.
		/// </summary>
		/// <param name="discreet">The discrete value to remove from the configuration. Cannot be null.</param>
		/// <returns>
		/// The current <see cref="DiscreteTextConfiguration"/> instance, allowing method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="discreet"/> is null.</exception>
		public DiscreteTextConfiguration RemoveDiscrete(TextDiscreet discreet)
		{
			if (discreet == null)
				throw new ArgumentNullException(nameof(discreet));

			discretes.RemoveAll(x => x.Equals(discreet));

			return this;
		}

		/// <summary>
		/// Replaces the current collection of discrete text values with the specified collection.
		/// </summary>
		/// <param name="discretes">The collection of discrete text values to set. Cannot be null.</param>
		/// <returns>
		/// The current <see cref="DiscreteTextConfiguration"/> instance, allowing method chaining.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="discretes"/> is null.</exception>
		public DiscreteTextConfiguration SetDiscretes(ICollection<TextDiscreet> discretes)
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
				discretes.Add(new TextDiscreet(parameter.Discretes[i], parameter.DiscreetDisplayValues[i]));
			}

			if (!parameter.HasDefaultStringValue())
			{
				DefaultValue = null;
			}
			else
			{
				DefaultValue = discretes.FirstOrDefault(x => x.Value.Equals(parameter.DefaultValue.StringValue));
			}
		}
	}
}
