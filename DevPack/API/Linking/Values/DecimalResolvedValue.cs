namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// A <see cref="ResolvedValue"/> whose resolved value is a <see cref="decimal"/>.
	/// </summary>
	public sealed class DecimalResolvedValue : ResolvedValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DecimalResolvedValue"/> class with the specified decimal value.
		/// </summary>
		/// <param name="value">The resolved decimal value.</param>
		public DecimalResolvedValue(decimal value)
		{
			Value = value;
		}

		/// <summary>Gets the resolved decimal value.</summary>
		public decimal Value { get; private set; }

		/// <inheritdoc />
		public override object GetRawValue()
		{
			return Value;
		}
	}
}
