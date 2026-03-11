namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents the base class for orchestration settings.
	/// </summary>
	public abstract class OrchestrationSettings : ApiObject
	{
		private protected OrchestrationSettings() : base()
		{
			IsNew = true;
		}

		private protected OrchestrationSettings(Guid orchestrationSettingId) : base(orchestrationSettingId)
		{
			IsNew = false;
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets the collection of capability settings.
		/// </summary>
		public abstract IReadOnlyCollection<CapabilitySetting> Capabilities { get; }

		/// <summary>
		/// Gets the collection of capacity settings.
		/// </summary>
		public abstract IReadOnlyCollection<CapacitySetting> Capacities { get; }

		/// <summary>
		/// Gets the collection of configuration settings.
		/// </summary>
		public abstract IReadOnlyCollection<ConfigurationSetting> Configurations { get; }

		/// <summary>
		/// Gets the collection of orchestration events.
		/// </summary>
		public abstract IReadOnlyCollection<OrchestrationEvent> OrchestrationEvents { get; }

		/// <summary>
		/// Adds a new capability.
		/// </summary>
		/// <param name="capabilitySetting">The capability setting to add.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings AddCapability(CapabilitySetting capabilitySetting);

		/// <summary>
		/// Removes the specified capability.
		/// </summary>
		/// <param name="capabilitySetting">The capability to remove. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings RemoveCapability(CapabilitySetting capabilitySetting);

		/// <summary>
		/// Sets the specified collection of capability settings.
		/// </summary>
		/// <param name="capabilitySettings">The capability settings to set. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySettings"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings SetCapabilities(IEnumerable<CapabilitySetting> capabilitySettings);

		/// <summary>
		/// Adds a new capacity.
		/// </summary>
		/// <param name="capacitySetting">The capacity setting to add.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings AddCapacity(CapacitySetting capacitySetting);

		/// <summary>
		/// Removes the specified capacity.
		/// </summary>
		/// <param name="capacitySetting">The capacity to remove. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings RemoveCapacity(CapacitySetting capacitySetting);

		/// <summary>
		/// Sets the specified collection of capacity settings.
		/// </summary>
		/// <param name="capacitySettings">The capacity settings to set. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySettings"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings SetCapacities(IEnumerable<CapacitySetting> capacitySettings);

		/// <summary>
		/// Adds a new configuration.
		/// </summary>
		/// <param name="configurationSetting">The configuration setting to add.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings AddConfiguration(ConfigurationSetting configurationSetting);

		/// <summary>
		/// Removes the specified configuration.
		/// </summary>
		/// <param name="configurationSetting">The configuration to remove. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSetting"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings RemoveConfiguration(ConfigurationSetting configurationSetting);

		/// <summary>
		/// Sets the specified collection of configuration settings.
		/// </summary>
		/// <param name="configurationSettings">The configuration settings to set. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="configurationSettings"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings SetConfigurations(IEnumerable<ConfigurationSetting> configurationSettings);

		/// <summary>
		/// Adds a new orchestration event.
		/// </summary>
		/// <param name="orchestrationEvent">The orchestration event to add.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="orchestrationEvent"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings AddOrchestrationEvent(OrchestrationEvent orchestrationEvent);

		/// <summary>
		/// Removes the specified orchestration event.
		/// </summary>
		/// <param name="orchestrationEvent">The orchestration event to remove. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="orchestrationEvent"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings RemoveOrchestrationEvent(OrchestrationEvent orchestrationEvent);

		/// <summary>
		/// Sets the specified collection of orchestration events.
		/// </summary>
		/// <param name="orchestrationEvents">The orchestration events to set. Cannot be null.</param>
		/// <returns>The current <see cref="OrchestrationSettings"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="orchestrationEvents"/> is <see langword="null"/>.</exception>
		public abstract OrchestrationSettings SetOrchestrationEvents(IEnumerable<OrchestrationEvent> orchestrationEvents);

		/// <summary>
		/// Checks if the provided object is an OrchestrationEvent instance and compares its properties to determine equality.
		/// </summary>
		/// <param name="obj">Object to compare.</param>
		/// <returns>True, if properties match, else false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not OrchestrationSettings other)
			{
				return false;
			}

			return Id == other.Id &&
				Name == other.Name &&
				Capabilities.ScrambledEquals(other.Capabilities) &&
				Capacities.ScrambledEquals(other.Capacities) &&
				Configurations.ScrambledEquals(other.Configurations) &&
				OrchestrationEvents.ScrambledEquals(other.OrchestrationEvents);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);

				foreach (var capability in Capabilities.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + capability.GetHashCode();
				}

				foreach (var capacity in Capacities.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + capacity.GetHashCode();
				}

				foreach (var configuration in Configurations.OrderBy(x => x.Id))
				{
					hash = (hash * 23) + configuration.GetHashCode();
				}

				foreach (var orchestrationEvent in OrchestrationEvents.OrderBy(x => x.EventType))
				{
					hash = (hash * 23) + orchestrationEvent.GetHashCode();
				}

				return hash;
			}
		}
	}
}
