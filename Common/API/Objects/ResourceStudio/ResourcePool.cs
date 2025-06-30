namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    public class ResourcePool : ApiObject
    {
        private StorageResourceStudio.ResourcepoolInstance originalInstance;

        private StorageResourceStudio.ResourcepoolInstance updatedInstance;

        private string name;

        public ResourcePool() : base()
        {
            IsNew = true;
        }

        public ResourcePool(Guid resourcePoolId) : base(resourcePoolId)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal ResourcePool(StorageResourceStudio.ResourcepoolInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
        }

        public string Name
        {
            get => name;
            set
            {
                HasChanges = true;
                name = value;
            }
        }

        public ResourcePoolState State { get; private set; }

        internal override bool IsNew { get; set; }

        internal override bool HasUserDefinedId { get; set; } = false;

        internal override bool HasChanges { get; set; } = false;

        internal StorageResourceStudio.ResourcepoolInstance OriginalInstance => originalInstance;

        internal StorageResourceStudio.ResourcepoolInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? ComposeNewInstance() : originalInstance.Clone();
            }

            updatedInstance.ResourcePoolInfo.Name = Name;

            return updatedInstance;
        }

        private void ParseInstance(StorageResourceStudio.ResourcepoolInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            Name = instance.ResourcePoolInfo.Name;
            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(instance.Status);
        }

        private StorageResourceStudio.ResourcepoolInstance ComposeNewInstance()
        {
            if (Id == Guid.Empty)
            {
                return new StorageResourceStudio.ResourcepoolInstance();
            }

            return new StorageResourceStudio.ResourcepoolInstance(Id);
        }
    }
}
