namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when creating or updating a resource pool with invalid configuration.
    /// </summary>
    public class ResourcePoolConfigurationError : MediaOpsErrorData
    {
        /// <summary>
		/// Specifies the reason for the error.
		/// </summary>
		public enum Reason
        {
            /// <summary>
            /// Indicates that a resource pool with the same name already exists.
            /// </summary>
            NameExists,

            /// <summary>
            /// Indicates that the name is invalid.
            /// </summary>
            InvalidName,

            /// <summary>
            /// Indicates that the ID is already in use by another object.
            /// </summary>
            IdInUse,

            /// <summary>
            /// Indicates that the value was already changed.
            /// </summary>
            ValueAlreadyChanged,

            /// <summary>
            /// Indicates that the input data contains a duplicate name.
            /// This can only occur when resource pools with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that a resource pool is in a state that does not allow the requested operation.
            /// </summary>
            InvalidState,

            /// <summary>
            /// Represents a response status indicating that the requested resource pool was not found.
            /// </summary>
            NotFound,
        }

        /// <summary>
        /// Gets the reason for the resource pool configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
