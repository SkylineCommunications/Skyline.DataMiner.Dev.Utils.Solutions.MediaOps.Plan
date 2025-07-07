namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Represents a resource pool in the MediaOps Plan API.
	/// </summary>
	public class ResourcePool : ApiObject
	{
		private StorageResourceStudio.ResourcepoolInstance originalInstance;

		private StorageResourceStudio.ResourcepoolInstance updatedInstance;

		private string name;

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePool"/> class.
		/// </summary>
		public ResourcePool() : base()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePool"/> class with a specific resource pool ID.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		public ResourcePool(Guid resourcePoolId) : base(resourcePoolId)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		internal ResourcePool(StorageResourceStudio.ResourcepoolInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(instance);
		}

		/// <summary>
		/// Gets or sets the name of the resource pool.
		/// </summary>
		public string Name
		{
			get => name;
			set
			{
				HasChanges = true;
				name = value;
			}
		}

		/// <summary>
		/// Gets the state of the resource pool.
		/// </summary>
		public ResourcePoolState State { get; private set; }

		internal override bool IsNew { get; set; }

		internal override bool HasUserDefinedId { get; set; } = false;

		internal override bool HasChanges { get; set; } = false;

		internal StorageResourceStudio.ResourcepoolInstance OriginalInstance => originalInstance;

		internal StorageResourceStudio.ResourcepoolInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageResourceStudio.ResourcepoolInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.ResourcePoolInfo.Name = Name;

			return updatedInstance;
		}

		private void ParseInstance(StorageResourceStudio.ResourcepoolInstance instance)
		{
			this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			name = instance.ResourcePoolInfo.Name;
			State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(instance.Status);
		}
	}
}
