namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an abstract base class for all settings that can hold either a direct value or a data reference.
    /// </summary>
    public abstract class Setting : TrackableObject
    {
        private protected Setting()
        {
        }

        private protected Setting(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            Id = id;

            IsNew = true;
        }

        private protected Setting(Setting setting)
        {
            Id = setting.Id;
            Reference = setting.Reference;

            IsNew = true;
        }

        /// <summary>
        /// Gets the unique identifier of the setting.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets or sets a reference to a data source that provides the value for this setting.
        /// </summary>
        public DataReference Reference { get; set; }

        /// <summary>
        /// Gets a value indicating whether this setting has a reference defined.
        /// </summary>
        public bool HasReference => Reference != null;

        /// <summary>
        /// Gets a value indicating whether this setting has a value defined.
        /// </summary>
        public abstract bool HasValue { get; }

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
    }
}
