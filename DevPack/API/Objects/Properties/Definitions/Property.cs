namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using static Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a property in the MediaOps Plan API.
	/// </summary>
	public abstract class Property : ApiObject
	{
		private StorageProperties.PropertyInstance originalInstance;
		private StorageProperties.PropertyInstance updatedInstance;

		private protected Property() : base()
		{
			IsNew = true;
		}

		private protected Property(Guid propertyId) : base(propertyId)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		private protected Property(StorageProperties.PropertyInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(instance);
		}

		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets or sets the scope of the property.
		/// </summary>
		public string Scope { get; set; }

		/// <summary>
		/// Gets or sets the name of the section to which the property belongs.
		/// </summary>
		public string SectionName { get; set; }

		/// <summary>
		/// Gets or sets the order of the property within its section.
		/// </summary>
		public int Order { get; set; }

		internal StorageProperties.PropertyInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
				hash = (hash * 23) + (Scope != null ? Scope.GetHashCode() : 0);
				hash = (hash * 23) + (SectionName != null ? SectionName.GetHashCode() : 0);
				hash = (hash * 23) + Order.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not Property other)
			{
				return false;
			}

			return Name == other.Name
				&& Scope == other.Scope
				&& SectionName == other.SectionName
				&& Order == other.Order;
		}

		internal abstract void ApplyChanges(StorageProperties.PropertyInstance instance);

		internal static Property InstantiateProperty(StorageProperties.PropertyInstance instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			return InstantiateProperties([instance]).FirstOrDefault();
		}

		internal static IEnumerable<Property> InstantiateProperties(IEnumerable<StorageProperties.PropertyInstance> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			if (!instances.Any())
			{
				return Enumerable.Empty<Property>();
			}

			return InstantiatePropertiesIterator(instances);
		}

		internal StorageProperties.PropertyInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageProperties.PropertyInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.PropertyInfo.Name = Name;
			updatedInstance.PropertyInfo.Scope = Scope;
			updatedInstance.Layout.SectionName = SectionName;
			updatedInstance.Layout.Order = Order;

			ApplyChanges(updatedInstance);

			return updatedInstance;
		}

		private static IEnumerable<Property> InstantiatePropertiesIterator(IEnumerable<StorageProperties.PropertyInstance> instances)
		{
			foreach (var instance in instances)
			{
				if (!instance.PropertyInfo.PropertyType.HasValue)
				{
					continue;
				}

				switch (instance.PropertyInfo.PropertyType.Value)
				{
					case StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.String: yield return new StringProperty(instance); break;
					case StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.Discrete: yield return new DiscreteProperty(instance); break;
					case StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.Boolean: yield return new BooleanProperty(instance); break;
					case StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.File: yield return new FileProperty(instance); break;

					default:
						continue;
				}
			}
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.PropertyInfo.Name;
			Scope = instance.PropertyInfo.Scope;
			SectionName = instance.Layout.SectionName;
			Order = instance.Layout.Order.HasValue ? (int)instance.Layout.Order.Value : 0;
		}
	}
}
