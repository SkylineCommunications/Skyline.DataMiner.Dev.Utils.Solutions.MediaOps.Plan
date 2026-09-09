namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	internal static class NodeReferences
	{
		/// <summary>
		/// Gets the identifier of the resource pool referenced by the node. A resource node references the resource
		/// pool the resource is used from, while a resource pool node references the resource pool directly.
		/// </summary>
		/// <param name="node">The node to get the resource pool identifier of.</param>
		/// <returns>The identifier of the referenced resource pool, or <see cref="Guid.Empty"/> when the node does not reference a resource pool.</returns>
		public static Guid GetResourcePoolId(INode node)
		{
			switch (node)
			{
				case IResourceNode resourceNode:
					return resourceNode.ResourcePoolId;
				case IResourcePoolNode resourcePoolNode:
					return resourcePoolNode.ResourcePoolId;
				default:
					return Guid.Empty;
			}
		}
	}
}
