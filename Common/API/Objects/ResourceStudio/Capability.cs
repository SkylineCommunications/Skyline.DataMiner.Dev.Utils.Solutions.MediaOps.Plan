namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a capability in the MediaOps.
    /// </summary>
    public class Capability : Parameter
    {
        private HashSet<string> discretes = new HashSet<string>();
        private bool isTimeDependent;
        private Guid linkedTimeDependentCapabilityId;

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class.
        /// </summary>
        public Capability() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capability.</param>
        public Capability(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize the capability. Must not be <see langword="null"/>.</param>
        internal protected Capability(CoreParameter parameter) : base(parameter)
        {
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
        /// <summary>
        /// Gets the category of the profile parameter, indicating its classification as a capability.
        /// </summary>
        protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Capability;

        internal Guid LinkedTimeDependentCapabilityId => linkedTimeDependentCapabilityId;

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

        /// <summary>
        /// Updates the collection of discrete options with the specified values.
        /// </summary>
        /// <param name="options">A collection of non-null, non-whitespace strings representing the new discrete options.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if any element in <paramref name="options"/> is <see langword="null"/>, empty, or consists only of
        /// whitespace.</exception>
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
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(CoreParameter parameter)
        {
            discretes = System.Linq.Enumerable.ToHashSet(parameter.Discretes);
            isTimeDependent = TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out var timeDependentLink) && timeDependentLink.IsTimeDependent;
            linkedTimeDependentCapabilityId = timeDependentLink?.LinkedParameterId ?? Guid.Empty;
        }
    }
}
