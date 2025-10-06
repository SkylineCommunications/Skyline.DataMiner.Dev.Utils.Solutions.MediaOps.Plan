namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Capacity in the MediaOps.
    /// </summary>
    public class Capacity : ApiObject
    {
        private string name;
        private bool isMandatory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class.
        /// </summary>
        public Capacity() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capacity.</param>
        public Capacity(Guid id) : base(id)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize the capacity. Must not be null.</param>
        internal protected Capacity(CoreParameter parameter) : base(parameter.ID)
        {
            ParseParameter(parameter);
        }

        /// <summary>
        /// Gets or sets the name of the capacity.
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                HasChanges = true;
                name = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the capacity is mandatory or not.
        /// </summary>
        public bool IsMandatory
        {
            get => isMandatory;
            set
            {
                HasChanges = true;
                isMandatory = value;
            }
        }

        private void ParseParameter(CoreParameter parameter)
        {
            if (parameter.Categories != ProfileParameterCategory.Capacity)
                throw new ArgumentException($"The provided parameter is not a {ProfileParameterCategory.Capacity}.", nameof(parameter));

            name = parameter.Name;
            isMandatory = parameter.IsOptional == false;
        }
    }
}
