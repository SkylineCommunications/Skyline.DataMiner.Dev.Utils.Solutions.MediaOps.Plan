namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when creating or updating a resource property with invalid configuration.
    /// </summary>
    public class ResourcePropertyConfigurationError : MediaOpsErrorData
    {
        /// <summary>
		/// Specifies the reason for the error.
		/// </summary>
        public enum Reason
        {
            /// <summary>
            /// Indicates that a resource property with the same name already exists.
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
            /// This can only occur when resource properties with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that the input data contains a duplicate ID.
            /// This can only occur when resource properties with the same ID are provided to a bulk operation.
            /// </summary>
            DuplicateId,

            /// <summary>
            /// Represents a response status indicating that the requested resource property was not found.
            /// </summary>
            NotFound,
        }

        /// <summary>
        /// Gets the reason for the resource property configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
