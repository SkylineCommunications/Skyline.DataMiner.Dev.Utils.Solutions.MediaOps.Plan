namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a difference between a capability configured in DOM and the corresponding capability in CORE.
	/// </summary>
	public sealed class CapabilityDifference : SynchronizationDifference
	{
		internal CapabilityDifference(SynchronizationDifferenceKind kind, Guid capabilityId, IEnumerable<string> domDiscretes, IEnumerable<string> coreDiscretes)
			: base(kind)
		{
			CapabilityId = capabilityId;
			DomDiscretes = new List<string>(domDiscretes ?? []).AsReadOnly();
			CoreDiscretes = new List<string>(coreDiscretes ?? []).AsReadOnly();
		}

		/// <summary>
		/// Gets the identifier of the profile parameter representing the capability.
		/// </summary>
		public Guid CapabilityId { get; }

		/// <summary>
		/// Gets a value indicating whether the capability is time dependent.
		/// </summary>
		public bool IsTimeDependent { get; internal set; }

		/// <summary>
		/// Gets the discrete values configured in DOM.
		/// </summary>
		public IReadOnlyCollection<string> DomDiscretes { get; }

		/// <summary>
		/// Gets the discrete values configured in CORE.
		/// </summary>
		public IReadOnlyCollection<string> CoreDiscretes { get; }
	}
}
