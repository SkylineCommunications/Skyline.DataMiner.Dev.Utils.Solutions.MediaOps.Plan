namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// A <see cref="ResolvedValue"/> whose resolved value is a <see cref="string"/>.
    /// </summary>
    public sealed class StringResolvedValue : ResolvedValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringResolvedValue"/> class with the specified string value.
        /// </summary>
        /// <param name="value">The resolved string value.</param>
        public StringResolvedValue(string value)
        {
            Value = value;
        }

        /// <summary>Gets the resolved string value.</summary>
        public string Value { get; private set; }

		/// <inheritdoc />
		public override object GetRawValue()
		{
			return Value;
		}
	}
}
