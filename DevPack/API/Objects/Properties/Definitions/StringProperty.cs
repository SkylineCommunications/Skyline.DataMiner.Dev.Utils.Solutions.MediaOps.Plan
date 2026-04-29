namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a string property in the MediaOps Plan API.
	/// </summary>
	public class StringProperty : Property
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringProperty"/> class.
		/// </summary>
		public StringProperty() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StringProperty"/> class with a specific property ID.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		public StringProperty(Guid propertyId) : base(propertyId)
		{
		}

		internal StringProperty(StorageProperties.PropertyInstance instance) : base(instance)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the default value of this <see cref="StringProperty"/>.
		/// </summary>
		public string DefaultValue { get; set; }

		/// <summary>
		/// Gets or sets the maximum allowed of characters. If not set, the default value is 250.
		/// </summary>
		public int SizeLimit { get; set; } = 250;

		/// <summary>
		/// Gets or sets a value indicating whether the content supports multiple lines.
		/// </summary>
		public bool IsMultiLine { get; set; } = false;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);
				hash = (hash * 23) + SizeLimit.GetHashCode();
				hash = (hash * 23) + IsMultiLine.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not StringProperty other)
			{
				return false;
			}

			return base.Equals(other)
				&& DefaultValue == other.DefaultValue
				&& SizeLimit == other.SizeLimit
				&& IsMultiLine == other.IsMultiLine;
		}

		internal override void ApplyChanges(StorageProperties.PropertyInstance instance)
		{
			instance.PropertyInfo.PropertyType = StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.String;
			instance.PropertyInfo.Default = DefaultValue;
			instance.PropertyInfo.StringSizeLimit = SizeLimit;
			instance.PropertyInfo.IsMultiLineString = IsMultiLine;
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			DefaultValue = instance.PropertyInfo.Default;
			SizeLimit = instance.PropertyInfo.StringSizeLimit.HasValue ? (int)instance.PropertyInfo.StringSizeLimit.Value : 250;
			IsMultiLine = instance.PropertyInfo.IsMultiLineString.HasValue && instance.PropertyInfo.IsMultiLineString.Value;
		}
	}
}
