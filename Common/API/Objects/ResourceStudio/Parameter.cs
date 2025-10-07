namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    public abstract class Parameter : ApiObject
    {
        private readonly CoreParameter coreParameter;
        private string name;
        private bool isMandatory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        internal protected Parameter() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration.</param>
        internal protected Parameter(Guid id) : base(id)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal protected Parameter(CoreParameter parameter) : base(parameter.ID)
        {
            coreParameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            ParseParameter();
        }

        protected internal abstract ProfileParameterCategory Category { get; }

        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        public sealed override string Name
        {
            get => name;
            set
            {
                HasChanges = true;
                name = value;
            }
        }

        public bool IsMandatory
        {
            get => isMandatory;
            set
            {
                HasChanges = true;
                isMandatory = value;
            }
        }

        internal CoreParameter CoreParameter => coreParameter;

        private void ParseParameter()
        {
            if (!coreParameter.Categories.HasFlag(Category))
                throw new InvalidOperationException($"The provided CORE parameter is not a {Category}.");

            name = coreParameter.Name;
            isMandatory = coreParameter.IsOptional == null || coreParameter.IsOptional == false;

            InternalParseParameter(coreParameter);
        }

        protected internal abstract void InternalParseParameter(CoreParameter parameter);
    }
}
