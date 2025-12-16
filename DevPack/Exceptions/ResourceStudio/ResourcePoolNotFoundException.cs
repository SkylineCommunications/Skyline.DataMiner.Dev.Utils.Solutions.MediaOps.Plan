namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception thrown when a resource pool with the specified identifier cannot be found.
    /// </summary>
    [Serializable]
    public class ResourcePoolNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolNotFoundException"/> class.
        /// </summary>
        public ResourcePoolNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolNotFoundException"/> class with a specified resource identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the resource pool that was not found.</param>
        public ResourcePoolNotFoundException(Guid id) : this($"No resource found with ID: {id}")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ResourcePoolNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolNotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ResourcePoolNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolNotFoundException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ResourcePoolNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}