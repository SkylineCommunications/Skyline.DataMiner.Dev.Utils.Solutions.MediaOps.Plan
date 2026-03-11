namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Provides access to system-level capabilities.
	/// </summary>
	public class SystemCapabilities
	{
		private readonly MediaOpsPlanApi planApi;

		private readonly Lazy<Capability> lazyResourceType;

		internal SystemCapabilities(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

			lazyResourceType = new Lazy<Capability>(() => GetOrCreateResourceType());
		}

		/// <summary>
		/// Gets the Resource Type capability.
		/// </summary>
		public Capability ResourceType => lazyResourceType.Value;

		private Capability GetOrCreateResourceType()
		{
			var resourceType = planApi.Capabilities.Read(CoreCapabilities.ResourceType.Id);
			if (resourceType == null)
			{
				var newCapability = new Capability(CoreCapabilities.ResourceType.Id)
				{
					Name = CoreCapabilities.ResourceType.Name,
				}
				.SetDiscretes(["Element", "Pool Resource", "Service", "Unlinked Resource", "Virtual Function",]);

				planApi.Capabilities.Create(newCapability);
				resourceType = planApi.Capabilities.Read(CoreCapabilities.ResourceType.Id);
			}

			return resourceType;
		}
	}
}
