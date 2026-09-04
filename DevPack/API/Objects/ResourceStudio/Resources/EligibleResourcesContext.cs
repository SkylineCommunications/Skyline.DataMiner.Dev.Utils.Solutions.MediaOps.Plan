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
		/// Gets or sets the ID of the job whose reservation usage must be ignored during the eligibility calculation.
		/// <see cref="Guid.Empty"/> by default.
		/// </summary>
		public Guid JobIdToIgnore { get; set; }

		/// <summary>
		/// Gets or sets the filter that restricts the resources considered for the eligibility request. Can be <see langword="null"/>.
		/// </summary>
		public FilterElement<Resource> Filter { get; set; }
	}
}
