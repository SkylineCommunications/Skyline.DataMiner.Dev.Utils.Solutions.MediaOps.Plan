namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException()
        {
        }

        public ResourceNotFoundException(Guid id) : this($"No resource found with ID: {id}")
        {
        }

        public ResourceNotFoundException(string message) : base(message)
        {
        }

        public ResourceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}