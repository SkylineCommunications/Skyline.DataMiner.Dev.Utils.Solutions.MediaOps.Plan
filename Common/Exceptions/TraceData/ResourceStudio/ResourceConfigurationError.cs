namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an error that occurs when creating or updating a resource with invalid configuration.
    /// </summary>
    public class ResourceConfigurationError : MediaOpsErrorData
    {
        /// <summary>
		/// Specifies the reason for the error.
		/// </summary>
		public enum Reason
        {
            /// <summary>
            /// Indicates that a resource with the same name already exists.
            /// </summary>
            NameExists,

            /// <summary>
            /// Indicates that the name is invalid.
            /// </summary>
            InvalidName,

            /// <summary>
            /// Indicates that the concurrency is invalid.
            /// </summary>
            InvalidConcurrency,

            /// <summary>
            /// Indicates that resource function related changes are not allowed.
            /// </summary>
            FunctionChangeNotAllowed,

            /// <summary>
            /// Indicates that changing the resource type is not allowed.
            /// </summary>
            TypeChangeNotAllowed,

            /// <summary>
            /// Indicates that the element link is invalid.
            /// </summary>
            InvalidElementLink,

            /// <summary>
            /// Indicates that the service link is invalid.
            /// </summary>
            InvalidServiceLink,

            /// <summary>
            /// Indicates that the function link is invalid.
            /// </summary>
            InvalidFunctionLink,

            /// <summary>
            /// Indicates that the table index link is invalid.
            /// </summary>
            InvalidTableIndexLink,

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
            /// This can only occur when resources with the same name are provided to a bulk operation.
            /// </summary>
            DuplicateName,

            /// <summary>
            /// Indicates that the input data contains a duplicate ID.
            /// This can only occur when resources with the same ID are provided to a bulk operation.
            /// </summary>
            DuplicateId,

            /// <summary>
            /// Indicates that a resource is in a state that does not allow the requested operation.
            /// </summary>
            InvalidState,

            /// <summary>
            /// Represents a response status indicating that the requested resource was not found.
            /// </summary>
            NotFound,
        }

        /// <summary>
        /// Gets the reason for the resource configuration error.
        /// </summary>
        public Reason ErrorReason { get; set; }
    }
}
