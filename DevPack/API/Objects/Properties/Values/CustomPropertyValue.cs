namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	/// <summary>
	/// Represents a custom property value that is not linked to a predefined property definition.
	/// </summary>
	public class CustomPropertyValue : PropertyValueBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomPropertyValue"/> class with the specified name.
		/// </summary>
		/// <param name="name">The name of the custom property. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is <see langword="null"/>.</exception>
		public CustomPropertyValue(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the string value of this custom property.
		/// </summary>
		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}
}
