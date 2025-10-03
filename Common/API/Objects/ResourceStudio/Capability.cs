namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a capability in the MediaOps.
    /// </summary>
    public class Capability : ApiObject
    {
        private readonly CoreParameter coreParameter;

        private string name;
        private bool isMandatory;
        private HashSet<string> discretes = new HashSet<string>();
        private bool isTimeDependent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class.
        /// </summary>
        public Capability() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capability.</param>
        public Capability(Guid id) : base(id)
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
            coreParameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
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

        /// <summary>
        /// Gets or sets a value indicating whether the capability is time-dependent or not.
        /// </summary>
        public bool IsTimeDependent
        {
            get => isTimeDependent;
            set
            {
                HasChanges = true;
                isTimeDependent = value;
            }
        }

        /// <summary>
        /// Gets a read-only collection of discrete values.
        /// </summary>
        public IReadOnlyCollection<string> Discretes => discretes;

        internal CoreParameter CoreParameter => coreParameter;

        /// <summary>
        /// Adds a discrete option to the collection if it is not already present.
        /// </summary>
        /// <param name="option">The discrete option to add. Cannot be <see langword="null"/> or whitespace.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="option"/> is <see langword="null"/> or whitespace.</exception>
        public void AddDiscrete(string option)
        {
            if (String.IsNullOrWhiteSpace(option))
                throw new ArgumentException(nameof(option));

            if (discretes.Add(option))
                HasChanges = true;
        }

        public void SetDiscretes(IEnumerable<string> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Any(x => String.IsNullOrWhiteSpace(x)))
                throw new ArgumentException(nameof(options));

            if (discretes.ScrambledEquals(options))
                return;

            discretes.Clear();
            foreach (var option in options)
            {
                discretes.Add(option);
            }

            HasChanges = true;
        }

        /// <summary>
        /// Removes the specified option from the collection of discretes.
        /// </summary>
        /// <param name="option">The option to remove. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="option"/> is <see langword="null"/>.</exception>
        public void RemoveDiscrete(string option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            if (discretes.Remove(option))
                HasChanges = true;
        }

        private void ParseParameter(CoreParameter parameter)
        {
            if (parameter.Categories != ProfileParameterCategory.Capability)
                throw new ArgumentException($"The provided parameter is not a {ProfileParameterCategory.Capability}.", nameof(parameter));

            name = parameter.Name;
            isMandatory = parameter.IsOptional == false;
            discretes = System.Linq.Enumerable.ToHashSet(parameter.Discretes);
            isTimeDependent = TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out var timeDependentLink) && timeDependentLink.IsTimeDependent;
        }
    }
}
