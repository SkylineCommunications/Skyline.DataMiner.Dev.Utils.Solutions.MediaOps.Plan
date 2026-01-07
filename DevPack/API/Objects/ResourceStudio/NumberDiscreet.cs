namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a discrete numeric value with an associated display name, using a decimal type for precision.
    /// </summary>
    public class NumberDiscreet : Discreet<decimal>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberDiscreet"/> class.
        /// </summary>
        public NumberDiscreet()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberDiscreet"/> class with the specified value and display name.
        /// </summary>
        /// <param name="value">Value of the <see cref="NumberDiscreet"/>.</param>
        /// <param name="displayName">Display name of the <see cref="NumberDiscreet"/>.</param>
        public NumberDiscreet(decimal value, string displayName) : base(value, displayName)
        {
            if (displayName == null)
                throw new ArgumentNullException(nameof(displayName));
        }
    }
}
