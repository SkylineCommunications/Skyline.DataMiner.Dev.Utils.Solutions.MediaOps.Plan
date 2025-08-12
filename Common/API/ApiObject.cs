namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;


    /// <summary>
    /// Represents the base class for all API objects in the MediaOps Plan API.
    /// </summary>
    public abstract class ApiObject
    {
        /// <summary>
        /// Gets the unique identifier of the API object.
        /// </summary>
        public Guid Id { get; private set; } // Should this be a Guid, could be a string?

        /// <summary>
        /// Gets the name of the API object.
        /// </summary>
        public abstract string Name { get; set; }

        private protected ApiObject()
            : this(Guid.NewGuid())
        {
        }

        private protected ApiObject(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
        }

        internal abstract bool IsNew { get; set; }

        internal abstract bool HasUserDefinedId { get; set; }

        internal abstract bool HasChanges { get; set; }
    }
}
