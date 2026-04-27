namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// A <see cref="LinkResolver"/> bound to a specific <see cref="ResourcePool"/>.
    /// </summary>
    /// <remarks>
    /// Use this resolver when references must be resolved against a known resource pool. The pool's
    /// <see cref="ResourcePool.OrchestrationSettings"/> are used as the default source for current-node
    /// configuration parameter values and names.
    /// </remarks>
    public class ResourcePoolLinkResolver : LinkResolver
    {
        private readonly OrchestrationSettings orchestrationSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolLinkResolver"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API used as the default data source.</param>
        /// <param name="resourcePool">The resource pool to resolve references against.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="planApi"/> or <paramref name="resourcePool"/> is <c>null</c>.</exception>
        public ResourcePoolLinkResolver(IMediaOpsPlanApi planApi, ResourcePool resourcePool)
            : base(planApi)
        {
            if (resourcePool == null)
                throw new ArgumentNullException(nameof(resourcePool));

            orchestrationSettings = resourcePool.OrchestrationSettings;
        }

        /// <inheritdoc/>
        protected override ResolvedValue TryResolveCurrentNodeCapability(CapabilityParameterReference reference, ResolveContext context)
            => ResolveFromOrchestrationSettings(orchestrationSettings, reference);

        /// <inheritdoc/>
        protected override ResolvedValue TryResolveCurrentNodeCapacity(CapacityParameterReference reference, ResolveContext context)
            => ResolveFromOrchestrationSettings(orchestrationSettings, reference);

        /// <inheritdoc/>
        protected override ResolvedValue TryResolveCurrentNodeConfiguration(ConfigurationParameterReference reference, ResolveContext context)
            => ResolveFromOrchestrationSettings(orchestrationSettings, reference);

        /// <inheritdoc/>
        protected override string TryGetCapabilityDisplayName(CapabilityParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetCapabilityDisplayName(reference, context);

        /// <inheritdoc/>
        protected override string TryGetCapacityDisplayName(CapacityParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetCapacityDisplayName(reference, context);

        /// <inheritdoc/>
        protected override string TryGetConfigurationDisplayName(ConfigurationParameterReference reference, ResolveContext context)
            => TryGetParameterNameFrom(orchestrationSettings, reference) ?? base.TryGetConfigurationDisplayName(reference, context);
    }
}
