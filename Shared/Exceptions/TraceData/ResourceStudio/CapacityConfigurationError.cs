namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when creating or updating a capacity with invalid configuration.
    /// </summary>
    public class CapacityConfigurationError : MediaOpsErrorData
    {
        /// <summary>
        /// Specifies the reason for the capacity configuration error.
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// Indicates that a capacity with the same name already exists.
            /// </summary>
            NameExists,

            /// <summary>
            /// Indicates that the name is invalid.
            /// </summary>
            InvalidName,

            /// <summary>
            /// Indicates that the provided ID is already assigned to another capacity
            /// </summary>
            IdInUse,

            /// <summary>
            /// Indicates that the step size is invalid.
            /// </summary>
            InvalidStepSize,

            /// <summary>
            /// Indicates that the minimum range is invalid.
            /// </summary>
            InvalidRangeMin,

            /// <summary>
            /// Indicates that the maximum range is invalid.
            /// </summary>
            InvalidRangeMax,

            /// <summary>
            /// Indicates that the number of decimals is invalid.
            /// </summary>
            InvalidDecimals,

            /// <summary>
            /// Indicates that the input data contains a duplicate name.
            /// This can only occur when capacities with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that the input data contains a duplicate ID.
            /// This can only occur when capacities with the same ID are provided to a bulk operation.
            /// </summary>
            DuplicateId,

            /// <summary>
            /// Indicates that a capacity is in a state that does not allow the requested operation.
            /// </summary>
            InvalidState,
        }

        /// <summary>
        /// Gets the reason for the capacity configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
