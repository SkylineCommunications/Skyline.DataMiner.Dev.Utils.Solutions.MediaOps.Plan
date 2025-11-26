namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when creating or updating a capability with invalid configuration.
    /// </summary>
    public class CapabilityConfigurationError : MediaOpsErrorData
    {
        /// <summary>
        /// Specifies the reason for the capability configuration error.
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// Indicates that a capability with the same name already exists.
            /// </summary>
            NameExists,

            /// <summary>
            /// Indicates that the name is invalid.
            /// </summary>
            InvalidName,

            /// <summary>
            /// Indicates that the discretes are invalid.
            /// </summary>
            InvalidDiscretes,

            /// <summary>
            /// Indicates that the provided ID is already assigned to another capability
            /// </summary>
            IdInUse,

            /// <summary>
            /// Indicates that the input data contains a duplicate name.
            /// This can only occur when capabilities with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that the input data contains a duplicate ID.
            /// This can only occur when capabilities with the same ID are provided to a bulk operation.
            /// </summary>
            DuplicateId,

            /// <summary>
            /// Indicates that the type is invalid.
            /// </summary>
            InvalidType,

            /// <summary>
            /// Indicates that the time-dependent value is invalid when attempting to change a capability from time-dependent to non-time-dependent or vice versa.
            /// </summary>
            InvalidTimeDependency,

            /// <summary>
            /// Indicates that a capability is in a state that does not allow the requested operation.
            /// </summary>
            InvalidState,
        }

        /// <summary>
        /// Gets the reason for the capability configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
