namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.General;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    public class ResourcePool : IApiObject
    {
        private StorageResourceStudio.ResourcepoolInstance instance;

        private string name;

        public ResourcePool()
        {
        }

        public ResourcePool(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Id cannot be empty.", nameof(id));
            }

            Id = id;
        }

        internal ResourcePool(StorageResourceStudio.ResourcepoolInstance instance)
        {
            ParseInstance(instance);
        }

        public Guid Id { get; private set; }

        public string Name
        {
            get => name;
            set
            {
                HasChanges = true;
                name = value;
            }
        }

        internal bool IsNew => instance != null;

        internal bool HasChanges{ get; set; } = false;

        internal StorageResourceStudio.ResourcepoolInstance Instance => instance;

        internal StorageResourceStudio.ResourcepoolInstance GetInstance()
        {
            var updatedInstance = IsNew ? ComposeNewInstance() : instance.Clone();

            instance.ResourcePoolInfo.Name = Name;

            return updatedInstance;
        }

        internal void ParseInstance(StorageResourceStudio.ResourcepoolInstance instance)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));

            Id = instance.ID.Id;
            Name = instance.ResourcePoolInfo.Name;
        }

        internal DomInstanceDifferences GetChanges()
        {
            var updatedinstance = GetInstance();

            return DomTools.CompareDomInstances(instance.ToInstance(), updatedinstance.ToInstance());
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
