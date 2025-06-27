namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    public abstract class ApiObject
    {
        internal ApiObject()
            : this(Guid.NewGuid())
        {
        }

        internal ApiObject(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
        }

        public Guid Id { get; private set; }

        internal abstract bool IsNew { get; set; }

        internal abstract bool HasUserDefinedId { get; set; }

        internal abstract bool HasChanges { get; set; }
    }
}
