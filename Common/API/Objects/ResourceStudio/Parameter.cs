namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    public abstract class Parameter : ApiObject
    {
        private CoreParameter originalParameter;
        private CoreParameter updatedParameter;
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
            ParseParameter(parameter);
        }

        protected internal abstract ProfileParameterCategory Category { get; }

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

        public bool IsMandatory
        {
            get => isMandatory;
            set
            {
                HasChanges = true;
                isMandatory = value;
            }
        }

        internal CoreParameter OriginalParameter => originalParameter;

        private void ParseParameter(CoreParameter parameter)
        {
            this.originalParameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            if (!parameter.Categories.HasFlag(Category))
                throw new ArgumentException($"The provided parameter is not a {Category}.", nameof(parameter));

            name = parameter.Name;
            isMandatory = parameter.IsOptional == false;

            InternalParseParameter(parameter);
        }

        protected internal abstract void InternalParseParameter(CoreParameter parameter);
    }
}
