namespace Skyline.DataMiner.MediaOps.Plan.API
{
    public class TrackableObject
    {
        protected internal TrackableObject()
        {
        }

        internal bool IsNew { get; set; }

        internal bool HasChanges { get; set; }
    }
}