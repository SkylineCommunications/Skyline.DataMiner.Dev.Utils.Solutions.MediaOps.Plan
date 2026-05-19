namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Thrown when <see cref="ReferenceResolver.ResolveValue"/> detects a circular chain of <see cref="DataReference"/> instances.
    /// </summary>
    public sealed class CircularReferenceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CircularReferenceException"/> class.
        /// </summary>
        /// <param name="reference">The reference at which the cycle was detected.</param>
        public CircularReferenceException(DataReference reference)
            : base($"Circular reference detected for reference of Type '{reference?.Type}'.")
        {
            Reference = reference;
        }

        /// <summary>
		/// Gets the reference at which the cycle was detected.
		/// </summary>
        public DataReference Reference { get; }
    }
}
