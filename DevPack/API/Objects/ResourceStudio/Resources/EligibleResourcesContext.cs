namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Describes the request for which the eligible <see cref="Resource"/> objects must be retrieved.
	/// </summary>
	public class EligibleResourcesContext
	{
		private IReadOnlyCollection<CapabilitySetting> capabilitySettings = Array.Empty<CapabilitySetting>();
		private IReadOnlyCollection<CapacitySetting> capacitySettings = Array.Empty<CapacitySetting>();

		/// <summary>
		/// Initializes a new instance of the <see cref="EligibleResourcesContext"/> class.
		/// </summary>
		/// <param name="start">The start of the time range for which the resources must be available.</param>
		/// <param name="end">The end of the time range for which the resources must be available.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="end"/> is earlier than <paramref name="start"/>.</exception>
		public EligibleResourcesContext(DateTimeOffset start, DateTimeOffset end)
		{
			if (end < start)
			{
				throw new ArgumentException("The end of the time range cannot be earlier than the start of the time range.", nameof(end));
			}

			Start = start;
			End = end;
		}

		/// <summary>
		/// Gets the start of the time range for which the resources must be available.
		/// </summary>
		public DateTimeOffset Start { get; }

		/// <summary>
		/// Gets the end of the time range for which the resources must be available.
		/// </summary>
		public DateTimeOffset End { get; }

		/// <summary>
		/// Gets or sets the capabilities the resources must provide. Empty by default.
		/// </summary>
		public IReadOnlyCollection<CapabilitySetting> CapabilitySettings
		{
			get => capabilitySettings;
			set => capabilitySettings = value ?? Array.Empty<CapabilitySetting>();
		}

		/// <summary>
		/// Gets or sets the capacities the resources must have available. Empty by default.
		/// </summary>
		public IReadOnlyCollection<CapacitySetting> CapacitySettings
		{
			get => capacitySettings;
			set => capacitySettings = value ?? Array.Empty<CapacitySetting>();
		}

		/// <summary>
		/// Gets or sets the filter that restricts the resources considered for the eligibility request. Can be <see langword="null"/>.
		/// </summary>
		public FilterElement<Resource> Filter { get; set; }

		/// <summary>
		/// Gets the identifier of this context, which is returned as <see cref="EligibleResourcesResult.ContextId"/>.
		/// </summary>
		public Guid Id { get; } = Guid.NewGuid();

		/// <summary>
		/// Gets or sets the job of which the booked usage of node <see cref="NodeIdToIgnore"/> is ignored, as if that node
		/// did not use its resource. Must be set together with <see cref="NodeIdToIgnore"/>.
		/// </summary>
		/// <remarks>The job must have a booking that contains the node.</remarks>
		public Guid JobIdToIgnore { get; set; }

		/// <summary>
		/// Gets or sets the node of job <see cref="JobIdToIgnore"/> of which the booked usage is ignored. Must be set together
		/// with <see cref="JobIdToIgnore"/>.
		/// </summary>
		public string NodeIdToIgnore { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the usage of every capacity of the eligible resources is returned, instead of
		/// only the usage of the requested capacities.
		/// </summary>
		public bool IncludeAllCapacityUsages { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the usage of every capability of the eligible resources is returned, instead
		/// of only the usage of the requested capabilities.
		/// </summary>
		public bool IncludeAllCapabilityUsages { get; set; }

		internal bool HasNodeToIgnore => JobIdToIgnore != Guid.Empty || !String.IsNullOrEmpty(NodeIdToIgnore);
	}
}
