namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// A <see cref="ResolvedValue"/> whose resolved value is a <see cref="double"/>.
	/// </summary>
	public sealed class DoubleResolvedValue : ResolvedValue
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DoubleResolvedValue"/> class with the specified double value.
		/// </summary>
		/// <param name="value">The resolved double value.</param>
		public DoubleResolvedValue(double value)
		{
			Value = value;
		}

		/// <summary>Gets the resolved double value.</summary>
		public double Value { get; private set; }

		/// <inheritdoc />
		public override object GetRawValue()
		{
			return Value;
		}
	}
}
