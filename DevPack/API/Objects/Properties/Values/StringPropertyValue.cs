namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a property value that holds a string value.
	/// </summary>
	public class StringPropertyValue : LinkedPropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StringPropertyValue"/> class linked to the specified string property.
		/// </summary>
		/// <param name="property">The <see cref="StringProperty"/> definition to link to.</param>
		public StringPropertyValue(StringProperty property) : base(property)
		{
		}

		internal StringPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the string value of this property.
		/// </summary>
		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}
}
