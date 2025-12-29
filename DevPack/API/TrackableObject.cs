namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an object that can be tracked for changes and state management.
    /// </summary>
    /// <remarks>This class is intended to be used as a base class for objects that require tracking of their
    /// state,  such as whether they are newly created or have been modified. It is not intended for direct
    /// instantiation  outside of derived classes.</remarks>
    public abstract class TrackableObject
    {
        private bool wasInitialized = false;
        private int initialHashCode = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackableObject"/> class.
        /// </summary>
        /// <remarks>This constructor is protected internal, allowing access from derived classes or
        /// classes within the same assembly.</remarks>
        protected internal TrackableObject()
        {
        }

        internal bool IsNew { get; set; }

        internal bool HasChanges
        {
            get
            {
                if (IsNew)
                {
                    return false;
                }

                if (!wasInitialized)
                {
                    throw new InvalidOperationException("Tracking for this object was never initialized");
                }

                return initialHashCode != GetHashCode();
            }
        }

        /// <summary>
        /// Generates the hash code for the object.
        /// </summary>
        /// <returns>Hash code representing the current object.</returns>
        public abstract override int GetHashCode();

        internal void InitTracking()
        {
            if (IsNew)
            {
                return;
            }

            if (wasInitialized)
            {
                throw new InvalidOperationException("Tracking was already initialized for this object");
            }

            initialHashCode = GetHashCode();
            wasInitialized = true;
        }
    }
}