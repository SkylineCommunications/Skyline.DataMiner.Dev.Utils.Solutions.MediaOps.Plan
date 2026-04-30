namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Thrown when <see cref="LinkResolver.GetValue"/> detects a circular chain of <see cref="DataReference"/> instances.
    /// </summary>
    [Serializable]
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

        private CircularReferenceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>Gets the reference at which the cycle was detected.</summary>
        public DataReference Reference { get; }
    }
}
