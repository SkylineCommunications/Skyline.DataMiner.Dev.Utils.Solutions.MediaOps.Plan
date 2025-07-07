namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    internal partial class ResourceInstance
    {
        protected override void BeforeToInstance()
        {
            ResourceInternalProperties.ApplyChanges();
        }
    }
}
