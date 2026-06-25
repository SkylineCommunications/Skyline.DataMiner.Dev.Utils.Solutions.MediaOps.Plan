namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Caches the reference definitions (capabilities, capacities, configurations, properties and resource
	/// properties) for the lifetime of a single API operation so that they are queried at most once and can be
	/// shared by every <see cref="ReferenceResolver"/> created during that operation.
	/// </summary>
	internal sealed class ReferenceDefinitionCache
	{
		private readonly Lazy<IDictionary<Guid, Capability>> lazyCapabilityDefinitions;
		private readonly Lazy<IDictionary<Guid, Capacity>> lazyCapacityDefinitions;
		private readonly Lazy<IDictionary<Guid, Configuration>> lazyConfigurationDefinitions;
		private readonly Lazy<IDictionary<Guid, Property>> lazyPropertyDefinitions;
		private readonly Lazy<IDictionary<Guid, ResourceProperty>> lazyResourcePropertyDefinitions;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceDefinitionCache"/> class.
		/// </summary>
		/// <param name="planApi">The API used to read the definitions.</param>
		public ReferenceDefinitionCache(IMediaOpsPlanApi planApi)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			lazyCapabilityDefinitions = new Lazy<IDictionary<Guid, Capability>>(() => planApi.Capabilities.Read().ToDictionary(c => c.Id));
			lazyCapacityDefinitions = new Lazy<IDictionary<Guid, Capacity>>(() => planApi.Capacities.Read().ToDictionary(c => c.Id));
			lazyConfigurationDefinitions = new Lazy<IDictionary<Guid, Configuration>>(() => planApi.Configurations.Read().ToDictionary(c => c.Id));
			lazyPropertyDefinitions = new Lazy<IDictionary<Guid, Property>>(() => planApi.Properties.Read().ToDictionary(c => c.Id));
			lazyResourcePropertyDefinitions = new Lazy<IDictionary<Guid, ResourceProperty>>(() => planApi.ResourceProperties.Read().ToDictionary(c => c.Id));
		}

		/// <summary>
		/// Gets the dictionary of capability definitions keyed by their identifier.
		/// </summary>
		public IDictionary<Guid, Capability> CapabilityDefinitions => lazyCapabilityDefinitions.Value;

		/// <summary>
		/// Gets the dictionary of capacity definitions keyed by their identifier.
		/// </summary>
		public IDictionary<Guid, Capacity> CapacityDefinitions => lazyCapacityDefinitions.Value;

		/// <summary>
		/// Gets the dictionary of configuration definitions keyed by their identifier.
		/// </summary>
		public IDictionary<Guid, Configuration> ConfigurationDefinitions => lazyConfigurationDefinitions.Value;

		/// <summary>
		/// Gets the dictionary of property definitions keyed by their identifier.
		/// </summary>
		public IDictionary<Guid, Property> PropertyDefinitions => lazyPropertyDefinitions.Value;

		/// <summary>
		/// Gets the dictionary of resource property definitions keyed by their identifier.
		/// </summary>
		public IDictionary<Guid, ResourceProperty> ResourcePropertyDefinitions => lazyResourcePropertyDefinitions.Value;
	}
}
