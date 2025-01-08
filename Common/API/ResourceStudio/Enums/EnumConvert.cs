namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    using DomHelpers.SlcResource_Studio;

    internal static class EnumConvert
    {
        public static ResourceStatus ConvertResourceStatus(string status)
        {
            switch (status)
            {
                case SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft:
                    return ResourceStatus.Draft;
                case SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete:
                    return ResourceStatus.Completed;
                case SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated:
                    return ResourceStatus.Deprecated;

                default:
                    throw new InvalidOperationException("Unknown status: " + status);
            }
        }

        public static string ConvertResourceStatus(ResourceStatus status)
        {
            switch (status)
            {
                case ResourceStatus.Draft:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft;
                case ResourceStatus.Completed:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete;
                case ResourceStatus.Deprecated:
                    return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated;

                default:
                    throw new InvalidOperationException("Unknown status: " + status);
            }
        }
    }
}
