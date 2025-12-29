namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Profiles;

    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a parameter in the API.
    /// </summary>
    /// <remarks>This abstract class serves as a base for specific parameter types, encapsulating common
    /// functionality such as name management, mandatory status, and category validation. Derived classes must implement
    /// the  <see cref="Category"/> property and the <see cref="InternalParseParameter(CoreParameter)"/> method to
    /// define specific behavior and parsing logic.</remarks>
    public abstract class Parameter : ApiObject
    {
        private readonly CoreParameter coreParameter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        private protected Parameter() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration.</param>
        private protected Parameter(Guid id) : base(id)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize this instance. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="parameter"/> is <see langword="null"/>.</exception>
        private protected Parameter(CoreParameter parameter) : base(parameter?.ID ?? throw new ArgumentNullException(nameof(parameter)))
        {
            coreParameter = parameter;
            ParseParameter();
        }

        /// <summary>
        /// Gets the category of the profile parameter.
        /// </summary>
        protected internal abstract ProfileParameterCategory Category { get; }

        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        public sealed override string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter is mandatory.
        /// </summary>
        public bool IsMandatory { get; set; }

        internal CoreParameter CoreParameter => coreParameter;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
                hash = (hash * 23) + IsMandatory.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Parameter instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current Parameter instance.</param>
        /// <returns>true if the specified object is a Parameter and has the same Id, Name, and IsMandatory values as the current
        /// instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not Parameter other)
                return false;

            return Id == other.Id &&
                   Name == other.Name &&
                   IsMandatory == other.IsMandatory;
        }

        /// <summary>
        /// Parses the specified parameter and applies any necessary transformations or validations.
        /// </summary>
        /// <remarks>This method is intended to be implemented by derived classes to handle
        /// parameter-specific parsing logic. Implementations should ensure that the provided <paramref
        /// name="parameter"/> is processed correctly and meets any required conditions for further use.</remarks>
        /// <param name="parameter">The parameter to be parsed. Must not be <see langword="null"/>.</param>
        protected internal abstract void InternalParseParameter(CoreParameter parameter);

        private void ParseParameter()
        {
            if (!coreParameter.Categories.HasFlag(Category))
                throw new InvalidOperationException($"The provided CORE parameter is not a {Category}.");

            Name = coreParameter.Name;
            IsMandatory = !coreParameter.IsOptional.HasValue || !coreParameter.IsOptional.Value;

            InternalParseParameter(coreParameter);
        }
    }
}
