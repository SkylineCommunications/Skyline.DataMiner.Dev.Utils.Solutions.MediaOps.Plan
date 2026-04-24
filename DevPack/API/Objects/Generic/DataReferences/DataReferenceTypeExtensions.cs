namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Extension methods for <see cref="DataReferenceType"/>.
    /// </summary>
    public static class DataReferenceTypeExtensions
    {
        /// <summary>
        /// Returns <c>true</c> when the reference type is scoped to a specific workflow node
        /// (i.e. it carries resource/scheduling information that differs per node).
        /// </summary>
        public static bool IsNodeScoped(this DataReferenceType type)
        {
            switch (type)
            {
                case DataReferenceType.ResourceName:
                case DataReferenceType.ResourceProperty:
                case DataReferenceType.ResourceLinkedObjectID:
                case DataReferenceType.ConfigurationParameter:
                    return true;
                default:
                    return false;
            }
        }
    }
}
