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
		/// Initializes a new instance of the <see cref="BooleanProperty"/> class with a specific property ID.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		public BooleanProperty(Guid propertyId) : base(propertyId)
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
			instance.PropertyInfo.Default = Convert.ToString(DefaultValue);
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			DefaultValue = bool.TryParse(instance.PropertyInfo.Default, out var result) ? result : false;
		}
	}
}
