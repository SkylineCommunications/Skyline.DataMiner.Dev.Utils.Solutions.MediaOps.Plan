namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    using DomHelpers;

    internal abstract class DomObject : IApiObject, IEquatable<DomObject>
    {
        protected DomObject(string defintionName, DomInstanceBase domInstance)
        {
            DomInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
            DefinitionName = defintionName ?? throw new ArgumentNullException(nameof(defintionName));
        }

        internal DomInstanceBase DomInstance { get; }

        public string DefinitionName { get; }

        public Guid Id => throw new NotImplementedException();

        ////public ApiObjectReference<T> Reference => new ApiObjectReference<T>(DomInstance.ID.Id);

        public override int GetHashCode()
        {
            return DomInstance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DomObject);
        }

        public virtual bool Equals(DomObject other)
        {
            if (other == null)
            {
                return false;
            }

            return DomInstance.Equals(other.DomInstance);
        }

        public override string ToString()
        {
            return $"{DefinitionName} [{Id}]";
        }
    }
}