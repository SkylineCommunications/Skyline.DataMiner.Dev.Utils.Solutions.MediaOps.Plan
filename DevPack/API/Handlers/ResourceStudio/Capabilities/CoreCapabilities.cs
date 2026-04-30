namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	internal static class CoreCapabilities
	{
		// Summary:
		//     Gets the capability definition for resource type.
		public static CoreCapabilityDefinition ResourceType { get; } = new CoreCapabilityDefinition(new Guid("f3995889-3d50-4972-8c9f-1eac9c663606"), "RST_ResourceType")
		{
			InternalUse = true,
		};

		// Summary:
		//     Gets an array of all capability definitions.
		public static CoreCapabilityDefinition[] AllCapabilities { get; } = [ResourceType];
	}
}
