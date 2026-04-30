namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a file property in the MediaOps Plan API.
	/// </summary>
	public class FileProperty : Property
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FileProperty"/> class.
		/// </summary>
		public FileProperty() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileProperty"/> class with a specific property ID.
		/// </summary>
		/// <param name="propertyId">The unique identifier of the property.</param>
		public FileProperty(Guid propertyId) : base(propertyId)
		{
		}

		internal FileProperty(StorageProperties.PropertyInstance instance) : base(instance)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets a value indicating whether there is a size limit for the file. If false, the limit configured on the server will be used.
		/// </summary>
		public bool HasSizeLimit { get; set; } = false;

		/// <summary>
		/// Gets or sets the maximum allowed size for the file, in MB. If not set, the default value is 20 MB.
		/// </summary>
		public long SizeLimit { get; set; } = 20;

		/// <summary>
		/// Gets or sets a value indicating whether multiple files are allowed.
		/// </summary>
		public bool AllowMultiple { get; set; } = false;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + HasSizeLimit.GetHashCode();
				hash = (hash * 23) + SizeLimit.GetHashCode();
				hash = (hash * 23) + AllowMultiple.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not FileProperty other)
			{
				return false;
			}
			return base.Equals(other)
				&& HasSizeLimit == other.HasSizeLimit
				&& SizeLimit == other.SizeLimit
				&& AllowMultiple == other.AllowMultiple;
		}

		internal override void ApplyChanges(StorageProperties.PropertyInstance instance)
		{
			instance.PropertyInfo.PropertyType = StorageProperties.SlcPropertiesIds.Enums.PropertytypeEnum.File;
			instance.PropertyInfo.FileSizeLimit = SizeLimit;
			instance.PropertyInfo.AllowMultipleFiles = AllowMultiple;
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			if (instance.PropertyInfo.FileSizeLimit.HasValue && instance.PropertyInfo.FileSizeLimit.Value > 0)
			{
				SizeLimit = instance.PropertyInfo.FileSizeLimit.Value;
				HasSizeLimit = true;
			}
			else
			{
				HasSizeLimit = false;
			}

			AllowMultiple = instance.PropertyInfo.AllowMultipleFiles.GetValueOrDefault();
		}
	}
}
