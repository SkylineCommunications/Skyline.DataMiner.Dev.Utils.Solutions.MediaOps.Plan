namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a discrete property in the MediaOps Plan API.
	/// </summary>
	public class DiscreteProperty : Property
	{
		private readonly List<string> discretes = new List<string>();

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscreteProperty"/> class.
		/// </summary>
		public DiscreteProperty() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscreteProperty"/> class with a specific property ID.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		public DiscreteProperty(Guid propertyId) : base(propertyId)
		{
		}

		internal DiscreteProperty(StorageProperties.PropertyInstance instance) : base(instance)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the default value of this <see cref="DiscreteProperty"/>.
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// Gets a read-only collection of discrete values.
		/// </summary>
		public IReadOnlyCollection<string> Discretes => discretes;

		/// <summary>
		/// Adds a discrete option to the collection.
		/// </summary>
		/// <param name="option">The discrete option to add. Cannot be <see langword="null"/>.</param>
		/// <returns>The current instance of the <see cref="DiscreteProperty"/> class, allowing for method chaining.</returns>
		/// <exception cref="ArgumentException">Thrown if <paramref name="option"/> is <see langword="null"/>.</exception>
		public DiscreteProperty AddDiscrete(string option)
		{
			if (option == null)
			{
				throw new ArgumentNullException(nameof(option));
			}

			discretes.Add(option);

			return this;
		}

		/// <summary>
		/// Removes the specified option from the collection of discretes.
		/// </summary>
		/// <param name="option">The option to remove. Cannot be <see langword="null"/>.</param>
		/// <returns>The current instance of the <see cref="DiscreteProperty"/> class, allowing for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="option"/> is <see langword="null"/>.</exception>
		public DiscreteProperty RemoveDiscrete(string option)
		{
			if (option == null)
			{
				throw new ArgumentNullException(nameof(option));
			}

			discretes.Remove(option);

			return this;
		}

		/// <summary>
		/// Updates the collection of discrete options with the specified values.
		/// </summary>
		/// <param name="options">A collection of non-null strings representing the new discrete options.</param>
		/// <returns>The current instance of the <see cref="DiscreteProperty"/> class, allowing for method chaining.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if any element in <paramref name="options"/> is <see langword="null"/>.</exception>
		public DiscreteProperty SetDiscretes(IEnumerable<string> options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			if (options.Any(x => x == null))
			{
				throw new ArgumentException(nameof(options));
			}

			if (discretes.ScrambledEquals(options))
			{
				return this;
			}


			discretes.Clear();
			foreach (var option in options)
			{
				discretes.Add(option);
			}

			return this;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);

				foreach (var discreet in discretes.OrderBy(x => x).ToArray())
				{
					hash = (hash * 23) + (discreet != null ? discreet.GetHashCode() : 0);
				}

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not DiscreteProperty other)
			{
				return false;
			}

			return base.Equals(other)
				&& DefaultValue == other.DefaultValue
				&& discretes.ScrambledEquals(other.discretes);
		}

		internal override void ApplyChanges(StorageProperties.PropertyInstance instance)
		{
			instance.PropertyInfo.PropertyType = StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.Discrete;
			instance.PropertyInfo.Default = DefaultValue;

			var toRemove = instance.Discrete.Where(d => !discretes.Contains(d.Option)).ToList();
			var toAdd = discretes.Where(d => !instance.Discrete.Any(s => s.Option == d)).ToList();

			foreach (var section in toRemove)
			{
				instance.Discrete.Remove(section);
			}

			foreach (var discrete in toAdd)
			{
				var newSection = new StorageProperties.DiscreteSection
				{
					Option = discrete
				};
				instance.Discrete.Add(newSection);
			}
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			DefaultValue = instance.PropertyInfo.Default;

			foreach (var discreteSection in instance.Discrete)
			{
				if (discreteSection.IsEmpty)
				{
					continue;
				}

				discretes.Add(discreteSection.Option);
			}
		}
	}
}
