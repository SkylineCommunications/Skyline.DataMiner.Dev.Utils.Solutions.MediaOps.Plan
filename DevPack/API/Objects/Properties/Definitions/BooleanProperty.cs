namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a boolean property in the MediaOps Plan API.
	/// </summary>
	public class BooleanProperty : Property
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanProperty"/> class.
		/// </summary>
		public BooleanProperty() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanProperty"/> class with the specified data.
		/// </summary>
		/// <param name="data">The data that can only be provided on creation.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a required field of <paramref name="data"/> is not filled out.</exception>
		public BooleanProperty(PropertyData data) : base(data)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanProperty"/> class with a specific property ID.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		public BooleanProperty(Guid propertyId) : base(propertyId)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BooleanProperty"/> class with a specific property ID and the specified data.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		/// <param name="data">The data that can only be provided on creation.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a required field of <paramref name="data"/> is not filled out.</exception>
		public BooleanProperty(Guid propertyId, PropertyData data) : base(propertyId, data)
		{
		}

		internal BooleanProperty(StorageProperties.PropertyInstance instance) : base(instance)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the default value of this <see cref="BooleanProperty"/>.
		/// </summary>
		public bool DefaultValue { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + DefaultValue.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not BooleanProperty other)
			{
				return false;
			}

			return base.Equals(other)
				&& DefaultValue == other.DefaultValue;
		}

		internal override void ApplyChanges(StorageProperties.PropertyInstance instance)
		{
			instance.PropertyInfo.PropertyType = StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.Boolean;
			instance.PropertyInfo.Default = Convert.ToString(DefaultValue);
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			DefaultValue = bool.TryParse(instance.PropertyInfo.Default, out var result) && result;
		}
	}
}
