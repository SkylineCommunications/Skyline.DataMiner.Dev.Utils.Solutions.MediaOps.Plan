namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic.DomQuerying
{
    using System;

    using DomHelpers;

    using Skyline.DataMiner.MediaOps.API.Common.API.Generic;

    internal abstract class DomObject<T> : IApiObject, IEquatable<DomObject<T>>
        where T : DomObject<T>
    {
        protected DomObject(string definitionName, DomInstanceBase domInstance)
        {
            DefinitionName = definitionName;
            DomInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
        }

        internal DomInstanceBase DomInstance { get; }

        string DefinitionName { get; }

        public Guid ID => throw new NotImplementedException();

        ////public ApiObjectReference<T> Reference => new ApiObjectReference<T>(DomInstance.ID.Id);

        public override int GetHashCode()
        {
            return DomInstance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DomObject<T>);
        }

        public virtual bool Equals(DomObject<T> other)
        {
            if (other == null)
            {
                return false;
            }

            return DomInstance.Equals(other.DomInstance);
        }

        public override string ToString()
        {
            return $"{DefinitionName} [{ID}]";
        }
    }
}