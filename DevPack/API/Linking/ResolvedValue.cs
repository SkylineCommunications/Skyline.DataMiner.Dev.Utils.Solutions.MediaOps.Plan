namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents the result of a <see cref="ReferenceResolver.ResolveValue"/> call.
    /// Either <see cref="Value"/> holds the resolved value, or <see cref="UnresolvedReference"/>
    /// holds the <see cref="DataReference"/> that could not be fully resolved.
    /// </summary>
    public sealed class ResolvedValue
    {
        private ResolvedValue()
        {
        }

        /// <summary>Gets the resolved value. Only valid when <see cref="IsResolved"/> is <c>true</c>.</summary>
        public object Value { get; private set; }

        /// <summary>Gets the unresolved reference. Only valid when <see cref="IsResolved"/> is <c>false</c>.</summary>
        public DataReference UnresolvedReference { get; private set; }

        /// <summary>Gets a value indicating whether the value was fully resolved.</summary>
        public bool IsResolved => UnresolvedReference == null;

        /// <summary>
        /// Creates a <see cref="ResolvedValue"/> wrapping a fully resolved value.
        /// </summary>
        /// <param name="value">The resolved value.</param>
        /// <returns>A resolved <see cref="ResolvedValue"/>.</returns>
        public static ResolvedValue FromValue(object value)
        {
            return new ResolvedValue { Value = value };
        }

        /// <summary>
        /// Creates a <see cref="ResolvedValue"/> wrapping a reference that could not be resolved any further.
        /// </summary>
        /// <param name="reference">The unresolved reference.</param>
        /// <returns>An unresolved <see cref="ResolvedValue"/>.</returns>
        public static ResolvedValue FromUnresolvedReference(DataReference reference)
        {
            return new ResolvedValue { UnresolvedReference = reference };
        }

		/// <summary>
		/// Creates a <see cref="ResolvedValue"/> by extracting the value from a concrete <see cref="Setting"/> subclass.
		/// </summary>
		/// <param name="setting">The setting whose value to extract.</param>
		/// <returns>A resolved <see cref="ResolvedValue"/>.</returns>
		public static ResolvedValue FromSettingValue(Setting setting)
        {
			return setting switch
			{
				NumberCapacitySetting ncs => FromValue(ncs.Value),
				RangeCapacitySetting rcs => FromValue(rcs.MaxValue),
				TextConfigurationSetting tcs => FromValue(tcs.Value),
				NumberConfigurationSetting nfcs => FromValue(nfcs.Value),
				DiscreteTextConfigurationSetting dtcs => FromValue(dtcs.Value?.Value),
				DiscreteNumberConfigurationSetting dncs => FromValue(dncs.Value?.Value),
				_ => FromValue(null),
			};
		}
    }
}
