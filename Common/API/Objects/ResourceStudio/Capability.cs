namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a capability in the MediaOps.
    /// </summary>
    public class Capability : ApiObject
    {
        private string name;
        private bool isMandatory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class.
        /// </summary>
        internal protected Capability() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capability.</param>
        internal protected Capability(Guid id) : base(id)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize the capability. Must not be <see langword="null"/>.</param>
        internal protected Capability(CoreParameter parameter) : base(parameter.ID)
        {
            ParseParameter(parameter);
        }

        /// <summary>
        /// Gets or sets the name of the capability.
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
        /// Gets or sets a value indicating whether the capability is mandatory or not.
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
            if (!parameter.Categories.HasFlag(ProfileParameterCategory.Capability))
                throw new ArgumentException($"The provided parameter is not a {ProfileParameterCategory.Capability}.", nameof(parameter));

            name = parameter.Name;
            isMandatory = parameter.IsOptional == false;
        }
    }
}
