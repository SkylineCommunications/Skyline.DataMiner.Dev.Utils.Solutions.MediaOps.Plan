namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    using System;

    internal partial class ResourcePoolInternalPropertiesSection
    {
        internal Guid ResourcePoolId
        {
            get
            {
                if (string.IsNullOrEmpty(Resource_Pool_Id) || !Guid.TryParse(Resource_Pool_Id, out var id))
                {
                    return Guid.Empty;
                }

                return id;
            }
            set
            {
                Resource_Pool_Id = value == Guid.Empty ? null : value.ToString();
            }
        }
    }
}
