namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal partial class ResourceInstance
    {
        protected override void BeforeToInstance()
        {
            ResourceInternalProperties.ApplyChanges();
        }

        public bool ClearError(string errorCode)
        {
            if (String.IsNullOrWhiteSpace(errorCode))
            {
                throw new ArgumentException("error code can't be empty", nameof(errorCode));
            }

            var errorsToRemove = Errors.Where(x => string.Equals(x.ErrorCode, errorCode)).ToList();
            if (!errorsToRemove.Any()) return false;

            foreach (var errorToRemove in errorsToRemove) Errors.Remove(errorToRemove);
            return true;
        }

        public IEnumerable<Guid> PoolIds
        {
            get
            {
                if (ResourceInternalProperties?.Pool_Ids == null)
                {
                    return null;
                }

                return ResourceInternalProperties.Pool_Ids.Split([";"], StringSplitOptions.RemoveEmptyEntries).Select(x => Guid.Parse(x));
            }
        }
    }
}
