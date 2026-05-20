namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// A <see cref="ResolvedValue"/> whose resolved value is a <see cref="bool"/>.
    /// </summary>
    public sealed class BooleanResolvedValue : ResolvedValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanResolvedValue"/> class with the specified boolean value.
        /// </summary>
        /// <param name="value">The resolved boolean value.</param>
        public BooleanResolvedValue(bool value)
        {
            Value = value;
        }

        /// <summary>Gets the resolved boolean value.</summary>
        public bool Value { get; private set; }

		/// <inheritdoc />
		public override object GetRawValue()
		{
			return Value;
		}
    }
}
