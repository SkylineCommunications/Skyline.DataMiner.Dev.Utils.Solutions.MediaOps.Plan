namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using StorageProperties = Storage.DOM.SlcProperties;

	public abstract class Property : ApiObject
	{
		private StorageProperties.PropertyInstance originalInstance;

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

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			Name = instance.PropertyInfo.Name;
		}
	}

	public class StringProperty : Property
	{
		public StringProperty() : base()
		{
		}
		public StringProperty(Guid propertyId) : base(propertyId)
		{
		}
		internal StringProperty(StorageProperties.PropertyInstance instance) : base(instance)
		{
			ParseInstance(instance);
			InitTracking();
		}

		public string DefaultValue { get; set; }

		public int StringSizeLimit { get; set; }

		public bool IsMultipleLine { get; set; }

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);
				hash = (hash * 23) + StringSizeLimit.GetHashCode();
				hash = (hash * 23) + IsMultipleLine.GetHashCode();

				return hash;
			}
		}

		public override bool Equals(object obj)
		{
			if (obj is not StringProperty other)
			{
				return false;
			}

			return base.Equals(other)
				&& DefaultValue == other.DefaultValue
				&& StringSizeLimit == other.StringSizeLimit
				&& IsMultipleLine == other.IsMultipleLine;
		}

		private void ParseInstance(StorageProperties.PropertyInstance instance)
		{
			DefaultValue = instance.PropertyInfo.Default;
			StringSizeLimit = instance.PropertyInfo.StringSizeLimit.HasValue ? (int)instance.PropertyInfo.StringSizeLimit.Value : 0;
			IsMultipleLine = instance.PropertyInfo.IsMultiLineString.HasValue ? instance.PropertyInfo.IsMultiLineString.Value : false;
		}
	}
}
