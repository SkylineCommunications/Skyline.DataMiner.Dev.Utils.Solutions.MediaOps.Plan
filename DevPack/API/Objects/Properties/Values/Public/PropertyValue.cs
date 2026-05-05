namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a property value that is linked to a specific <see cref="Property"/> definition.
	/// </summary>
	public abstract class PropertyValue : PropertyValueBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValue"/> class linked to the specified property.
		/// </summary>
		/// <param name="property">The property definition to link to. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="property"/> is <see langword="null"/>.</exception>
		public PropertyValue(Property property)
		{
			PropertyId = property?.Id ?? throw new ArgumentNullException(nameof(property));
		}

		internal PropertyValue()
		{
		}

		/// <summary>
		/// Gets the unique identifier of the linked property definition.
		/// </summary>
		public Guid PropertyId { get; internal set; }
	}
}
