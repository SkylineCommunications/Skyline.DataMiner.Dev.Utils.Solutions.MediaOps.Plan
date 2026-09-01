namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a property in the MediaOps Plan API.
	/// </summary>
	/// <seealso cref="BooleanProperty"/>
	/// <seealso cref="DiscreteProperty"/>
	/// <seealso cref="FileProperty"/>
	/// <seealso cref="StringProperty"/>
	public abstract class Property : ApiNamedObject
	{
		private StorageProperties.PropertyInstance originalInstance;
		private StorageProperties.PropertyInstance updatedInstance;

		private protected Property() : base()
		{
			IsNew = true;
		}

		private protected Property(PropertyData data) : base()
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			IsNew = true;
			Scope = data.Scope;
		}

		private protected Property(Guid propertyId) : base(propertyId)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		private protected Property(Guid propertyId, PropertyData data) : base(propertyId)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			IsNew = true;
			HasUserDefinedId = true;
			Scope = data.Scope;
		}

		private protected Property(StorageProperties.PropertyInstance instance) : base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
		{
			Scope = instance.PropertyInfo.Scope;
			ParseInstance(instance);
		}

		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Gets the scope of the property. The scope can only be provided through a <see cref="PropertyData"/> instance when the property is created.
		/// </summary>
		public string Scope { get; private set; }

		/// <summary>
		/// Gets or sets the name of the section to which the property belongs.
		/// </summary>
		public string SectionName { get; set; }

		/// <summary>
		/// Gets or sets the order of the property within its section.
		/// </summary>
		public int Order { get; set; }

		internal StorageProperties.PropertyInstance OriginalInstance => originalInstance;

		/// <summary>
		/// Determines whether this property is a boolean property and, if so, returns it as a <see cref="BooleanProperty"/>.
		/// </summary>
		/// <param name="property">When this method returns, contains the current property as a <see cref="BooleanProperty"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property is a <see cref="BooleanProperty"/>; otherwise, <c>false</c>.</returns>
		public bool IsBooleanProperty(out BooleanProperty property)
		{
			property = this as BooleanProperty;
			return property != null;
		}

		/// <summary>
		/// Determines whether this property is a discrete property and, if so, returns it as a <see cref="DiscreteProperty"/>.
		/// </summary>
		/// <param name="property">When this method returns, contains the current property as a <see cref="DiscreteProperty"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property is a <see cref="DiscreteProperty"/>; otherwise, <c>false</c>.</returns>
		public bool IsDiscreteProperty(out DiscreteProperty property)
		{
			property = this as DiscreteProperty;
			return property != null;
		}

		/// <summary>
		/// Determines whether this property is a file property and, if so, returns it as a <see cref="FileProperty"/>.
		/// </summary>
		/// <param name="property">When this method returns, contains the current property as a <see cref="FileProperty"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property is a <see cref="FileProperty"/>; otherwise, <c>false</c>.</returns>
		public bool IsFileProperty(out FileProperty property)
		{
			property = this as FileProperty;
			return property != null;
		}

		/// <summary>
		/// Determines whether this property is a string property and, if so, returns it as a <see cref="StringProperty"/>.
		/// </summary>
		/// <param name="property">When this method returns, contains the current property as a <see cref="StringProperty"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this property is a <see cref="StringProperty"/>; otherwise, <c>false</c>.</returns>
		public bool IsStringProperty(out StringProperty property)
		{
			property = this as StringProperty;
			return property != null;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
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

			return Id == other.Id
				&& Name == other.Name
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

		internal void AssignScope(string scope)
		{
			if (string.IsNullOrWhiteSpace(scope))
			{
				throw new ArgumentException("Scope cannot be null or whitespace.", nameof(scope));
			}

			if (!IsNew)
			{
				throw new InvalidOperationException("Scope can only be assigned to new properties.");
			}

			if (!string.IsNullOrEmpty(Scope))
			{
				throw new InvalidOperationException("Scope has already been assigned and cannot be modified.");
			}

			Scope = scope;
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
