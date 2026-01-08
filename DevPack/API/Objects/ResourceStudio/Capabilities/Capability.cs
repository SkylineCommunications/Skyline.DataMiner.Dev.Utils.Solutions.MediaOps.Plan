namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Live.Orchestration.Scheduling;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Profiles;

    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a capability in the MediaOps Plan API.
    /// </summary>
    public class Capability : Parameter
    {
        private readonly HashSet<string> discretes = new HashSet<string>();
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
            InitTracking();
        }

        /// <summary>
        /// Defines an implicit conversion from a Capability instance to its underlying Guid identifier.
        /// </summary>
        /// <remarks>This operator enables a Capability object to be used wherever a Guid is expected,
        /// returning the value of its Id property. If the Capability instance is null, a NullReferenceException will be
        /// thrown.</remarks>
        /// <param name="capability">The Capability instance to convert to a Guid.</param>
        public static implicit operator Guid(Capability capability) => capability.Id;

        /// <summary>
        /// Gets or sets a value indicating whether the capability is time-dependent or not.
        /// </summary>
        public bool IsTimeDependent { get; set; }

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
        public Capability AddDiscrete(string option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            discretes.Add(option);

            return this;
        }

        /// <summary>
        /// Removes the specified option from the collection of discretes.
        /// </summary>
        /// <param name="option">The option to remove. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="option"/> is <see langword="null"/>.</exception>
        public Capability RemoveDiscrete(string option)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            discretes.Remove(option);

            return this;
        }

        /// <summary>
        /// Updates the collection of discrete options with the specified values.
        /// </summary>
        /// <param name="options">A collection of non-null, non-whitespace strings representing the new discrete options.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if any element in <paramref name="options"/> is <see langword="null"/>, empty, or consists only of
        /// whitespace.</exception>
        public Capability SetDiscretes(IEnumerable<string> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Any(x => x == null))
                throw new ArgumentException(nameof(options));

            if (discretes.ScrambledEquals(options))
                return this;

            discretes.Clear();
            foreach (var option in options)
            {
                discretes.Add(option);
            }

            return this;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + IsTimeDependent.GetHashCode();
                foreach (var discreet in discretes.OrderBy(x => x).ToArray())
                {
                    hash = (hash * 23) + (discreet != null ? discreet.GetHashCode() : 0);
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not Capability other)
                return false;

            return base.Equals(other) && IsTimeDependent == other.IsTimeDependent && discretes.ScrambledEquals(other.discretes);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(CoreParameter parameter)
        {
            discretes.Clear();
            foreach (var discreet in parameter.Discretes)
            {
                discretes.Add(discreet);
            }

            IsTimeDependent = TimeDependentCapabilityLink.TryDeserialize(parameter.Remarks, out var timeDependentLink) && timeDependentLink.IsTimeDependent;
            linkedTimeDependentCapabilityId = timeDependentLink?.LinkedParameterId ?? Guid.Empty;
        }
    }
}
