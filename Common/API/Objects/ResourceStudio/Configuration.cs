namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Configuration in the MediaOps.
    /// </summary>
    public abstract class Configuration : ApiObject
    {
        private string name;
        private bool isMandatory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        internal protected Configuration() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration.</param>
        internal protected Configuration(Guid id) : base(id)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to configure the instance. Must not be <see langword="null"/>.</param>
        internal protected Configuration(CoreParameter parameter) : base(parameter.ID)
        {
            ParseParameter(parameter);
        }

        /// <summary>
        /// Gets or sets the name of the configuration.
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
        /// Gets or sets a value indicating whether the configuration is mandatory or not.
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
            if (parameter.Categories != ProfileParameterCategory.Configuration)
                throw new ArgumentException($"The provided parameter is not a {ProfileParameterCategory.Configuration}.", nameof(parameter));

            name = parameter.Name;
            isMandatory = parameter.IsOptional == false;

            InternalParseParameter(parameter);
        }

        /// <summary>
        /// Parses the specified parameter and applies any necessary transformations or validations.
        /// </summary>
        /// <remarks>This method is intended to be implemented by derived classes to handle
        /// parameter-specific parsing logic.</remarks>
        /// <param name="parameter">The parameter to be parsed. Must not be <see langword="null"/>.</param>
        protected internal abstract void InternalParseParameter(CoreParameter parameter);
    }
}
