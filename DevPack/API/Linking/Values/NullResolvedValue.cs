namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// A <see cref="ResolvedValue"/> that carries no value.
    /// </summary>
    public sealed class NullResolvedValue : ResolvedValue
    {
        /// <summary>Initializes a new instance of the <see cref="NullResolvedValue"/> class.</summary>
        public NullResolvedValue()
        {
        }

		/// <inheritdoc />
		public override object GetRawValue()
		{
			return null;
		}
	}
}
