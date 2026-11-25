namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Threading;

	/// <summary>
	/// Holds the orchestration settings of an owning object (e.g. a job or one of its nodes) and defers reading them until
	/// they are accessed for the first time.
	/// </summary>
	/// <typeparam name="TSettings">The type of the orchestration settings.</typeparam>
	/// <remarks>
	/// The ID of the stored settings is known without reading them, so it can be written back on save while the settings
	/// themselves were never loaded.
	/// </remarks>
	internal sealed class OrchestrationSettingsHolder<TSettings>
		where TSettings : OrchestrationSettings
	{
		private readonly object sync = new object();
		private readonly Guid storedId;
		private readonly Guid trackingId;
		private readonly Func<Guid, TSettings> resolver;
		private readonly Func<TSettings> createEmpty;

		private TSettings value;
		private int initialHashOfNewSettings;

		private OrchestrationSettingsHolder(Guid storedId, Func<Guid, TSettings> resolver, Func<TSettings> createEmpty)
		{
			this.storedId = storedId;
			trackingId = storedId;
			this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
			this.createEmpty = createEmpty ?? throw new ArgumentNullException(nameof(createEmpty));
		}

		private OrchestrationSettingsHolder(TSettings value)
		{
			this.value = value ?? throw new ArgumentNullException(nameof(value));
			storedId = value.Id;
			trackingId = value.Id;
			initialHashOfNewSettings = value.GetHashCode();
		}

		/// <summary>
		/// Gets the ID of the settings, without loading them.
		/// </summary>
		internal Guid Id => Volatile.Read(ref value)?.Id ?? storedId;

		/// <summary>
		/// Gets the settings, reading them on first access.
		/// </summary>
		internal TSettings Value
		{
			get
			{
				var current = Volatile.Read(ref value);
				if (current != null)
				{
					return current;
				}

				lock (sync)
				{
					if (value == null)
					{
						var loaded = resolver(storedId);
						if (loaded == null)
						{
							// The stored settings no longer exist, so the owner starts from empty settings like before.
							loaded = createEmpty();
							initialHashOfNewSettings = loaded.GetHashCode();
						}

						Volatile.Write(ref value, loaded);
					}

					return value;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether loaded settings were changed. Settings that were never loaded cannot have changed.
		/// </summary>
		internal bool HasChanges
		{
			get
			{
				var current = Volatile.Read(ref value);
				if (current == null)
				{
					return false;
				}

				// HasChanges always reports false for new settings, so their content is compared instead.
				return current.IsNew
					? current.GetHashCode() != initialHashOfNewSettings
					: current.HasChanges;
			}
		}

		/// <summary>
		/// Creates a holder for settings that are already available.
		/// </summary>
		/// <param name="value">The settings.</param>
		/// <returns>The holder.</returns>
		internal static OrchestrationSettingsHolder<TSettings> FromValue(TSettings value)
		{
			return new OrchestrationSettingsHolder<TSettings>(value);
		}

		/// <summary>
		/// Creates a holder that reads the stored settings on first access. An empty <paramref name="storedId"/> yields
		/// empty settings without reading anything.
		/// </summary>
		/// <param name="storedId">The ID of the stored settings.</param>
		/// <param name="resolver">Reads the settings with the given ID, returning <see langword="null"/> when they do not exist.</param>
		/// <param name="createEmpty">Creates empty settings.</param>
		/// <returns>The holder.</returns>
		internal static OrchestrationSettingsHolder<TSettings> Deferred(Guid? storedId, Func<Guid, TSettings> resolver, Func<TSettings> createEmpty)
		{
			if (createEmpty == null)
			{
				throw new ArgumentNullException(nameof(createEmpty));
			}

			if (!storedId.HasValue || storedId.Value == Guid.Empty)
			{
				return FromValue(createEmpty());
			}

			return new OrchestrationSettingsHolder<TSettings>(storedId.Value, resolver, createEmpty);
		}

		/// <summary>
		/// Returns the hash code the owner should include for these settings. While change tracking computes its hash,
		/// only the identity of the settings is used so tracking never forces the settings to be read; changes to loaded
		/// settings are reported through <see cref="HasChanges"/> instead.
		/// </summary>
		/// <returns>The hash code.</returns>
		internal int GetOwnerHashCode()
		{
			return TrackableObject.IsComputingTrackingHash
				? trackingId.GetHashCode()
				: Value.GetHashCode();
		}
	}
}
