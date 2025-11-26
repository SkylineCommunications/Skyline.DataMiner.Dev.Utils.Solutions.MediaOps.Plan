namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents an object that can be tracked for changes and state management.
    /// </summary>
    /// <remarks>This class is intended to be used as a base class for objects that require tracking of their
    /// state,  such as whether they are newly created or have been modified. It is not intended for direct
    /// instantiation  outside of derived classes.</remarks>
    public class TrackableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackableObject"/> class.
        /// </summary>
        /// <remarks>This constructor is protected internal, allowing access from derived classes or
        /// classes within the same assembly.</remarks>
        protected internal TrackableObject()
        {
        }

        internal bool IsNew { get; set; }

        internal bool HasChanges { get; set; }
    }
}