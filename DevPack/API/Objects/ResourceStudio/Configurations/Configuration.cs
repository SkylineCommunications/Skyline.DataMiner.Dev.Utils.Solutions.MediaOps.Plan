namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreParameter = Net.Profiles.Parameter;

	/// <summary>
	/// Represents a Configuration in the MediaOps.
	/// </summary>
	public abstract class Configuration : Parameter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Configuration"/> class.
		/// </summary>
		private protected Configuration() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Configuration"/> class with the specified unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier for the configuration.</param>
		private protected Configuration(Guid id) : base(id)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Configuration"/> class using the specified core parameter.
		/// </summary>
		/// <param name="parameter">The core parameter used to configure the instance. Must not be <see langword="null"/>.</param>
		private protected Configuration(CoreParameter parameter) : base(parameter)
		{
		}

		/// <summary>
		/// Gets the category of the profile parameter, indicating its classification as a configuration.
		/// </summary>
		protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Configuration;

		/// <summary>
		/// Determines whether this configuration represents a numeric configuration and, if so, returns it as a <see cref="NumberConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as a <see cref="NumberConfiguration"/> when it represents a numeric configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> if the current configuration instance is of type <see cref="NumberConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsNumberConfiguration(out NumberConfiguration configuration)
		{
			configuration = this as NumberConfiguration;
			return configuration != null;
		}

		/// <summary>
		/// Determines whether this configuration represents a discrete numeric configuration and, if so, returns it as a <see cref="DiscreteNumberConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as a <see cref="DiscreteNumberConfiguration"/> when it represents a discrete numeric configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> if the current configuration instance is of type <see cref="DiscreteNumberConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsDiscreteNumberConfiguration(out DiscreteNumberConfiguration configuration)
		{
			configuration = this as DiscreteNumberConfiguration;
			return configuration != null;
		}

		/// <summary>
		/// Determines whether this configuration represents a text configuration and, if so, returns it as a <see cref="TextConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as a <see cref="TextConfiguration"/> when it represents a text configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> if the current configuration instance is of type <see cref="TextConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsTextConfiguration(out TextConfiguration configuration)
		{
			configuration = this as TextConfiguration;
			return configuration != null;
		}

		/// <summary>
		/// Determines whether this configuration represents a discrete text configuration and, if so, returns it as a <see cref="DiscreteTextConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as a <see cref="DiscreteTextConfiguration"/> when it represents a discrete text configuration; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> if the current configuration instance is of type <see cref="DiscreteTextConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsDiscreteTextConfiguration(out DiscreteTextConfiguration configuration)
		{
			configuration = this as DiscreteTextConfiguration;
			return configuration != null;
		}

		internal static Configuration InstantiateConfiguration(CoreParameter instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			return InstantiateConfigurations([instance]).FirstOrDefault();
		}

		/// <summary>
		/// Creates configuration instances from the provided collection of core parameter instances.
		/// </summary>
		/// <param name="instances">The collection of core parameter instances to instantiate as configurations.</param>
		/// <returns>An enumerable collection of configuration objects created from the instances.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="instances"/> is <c>null</c>.</exception>
		internal static IEnumerable<Configuration> InstantiateConfigurations(IEnumerable<CoreParameter> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			if (!instances.Any())
			{
				return [];
			}

			return InstantiateConfigurationsIterator(instances);
		}

		/// <summary>
		/// Iterator method that creates configuration instances from the provided collection of core parameter instances.
		/// </summary>
		/// <param name="instances">The collection of core parameter instances to process.</param>
		/// <returns>An enumerable collection of configuration objects.</returns>
		private static IEnumerable<Configuration> InstantiateConfigurationsIterator(IEnumerable<CoreParameter> instances)
		{
			foreach (var instance in instances)
			{
				if (!instance.IsConfiguration())
				{
					continue;
				}

				if (instance.IsText())
				{
					yield return new TextConfiguration(instance);
				}
				else if (instance.IsNumber())
				{
					yield return new NumberConfiguration(instance);
				}
				else if (instance.IsTextDiscreet())
				{
					yield return new DiscreteTextConfiguration(instance);
				}
				else if (instance.IsNumberDiscreet())
				{
					yield return new DiscreteNumberConfiguration(instance);
				}
				else
				{
					// continue
				}
			}
		}
	}
}
