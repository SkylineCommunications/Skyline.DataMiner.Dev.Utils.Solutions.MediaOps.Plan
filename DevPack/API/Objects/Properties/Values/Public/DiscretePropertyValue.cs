namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a property value that holds a discrete (predefined) string value.
	/// </summary>
	public class DiscretePropertyValue : PropertyValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiscretePropertyValue"/> class linked to the specified discrete property.
		/// </summary>
		/// <param name="property">The <see cref="DiscreteProperty"/> definition to link to.</param>
		public DiscretePropertyValue(DiscreteProperty property) : base(property)
		{
		}

		internal DiscretePropertyValue()
		{
		}

		/// <summary>
		/// Gets or sets the discrete value of this property.
		/// </summary>
		public string Value { get; set; }
	}
}
