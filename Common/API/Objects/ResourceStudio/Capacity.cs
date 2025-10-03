namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    public class Capacity : ApiObject
    {
        private CoreParameter originalParameter;

        private CoreParameter updatedParameter;

        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class.
        /// </summary>
        public Capacity() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class with a specific capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier of the capacity.</param>
        public Capacity(Guid capacityId) : base(capacityId)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal Capacity(CoreParameter parameter) : base(parameter.ID)
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

        private void ParseParameter(CoreParameter parameter)
        {
            originalParameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }
    }
}
