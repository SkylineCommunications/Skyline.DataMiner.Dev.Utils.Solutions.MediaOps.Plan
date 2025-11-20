namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an error that occurs when creating or updating a configuration with invalid configuration.
    /// </summary>
    public class ConfigurationConfigurationError : MediaOpsErrorData
    {
        /// <summary>
        /// Specifies the reason for the configuration error.
        /// </summary>
        public enum Reason
        {
            /// <summary>
            /// Indicates that a configuration with the same name already exists.
            /// </summary>
            NameExists,

            /// <summary>
            /// Indicates that the name is invalid.
            /// </summary>
            InvalidName,

            /// <summary>
            /// Indicates that the provided ID is already assigned to another configuration
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
            /// This can only occur when configurations with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that the input data contains a duplicate ID.
            /// This can only occur when configurations with the same ID are provided to a bulk operation.
            /// </summary>
            DuplicateId,

            /// <summary>
            /// Indicates that a configuration is in a state that does not allow the requested operation.
            /// </summary>
            InvalidState,

            /// <summary>
            /// Indicates that there is an issue with the default value.
            /// </summary>
            InvalidDefaultValue,

            /// <summary>
            /// Indicates that there is an issue with the default discreet value.
            /// </summary>
            InvalidDefaultDiscreet,

            /// <summary>
            /// Indicates that there is an issue with the discreet values.
            /// </summary>
            InvalidDiscretes,
        }

        /// <summary>
        /// Gets the reason for the configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
