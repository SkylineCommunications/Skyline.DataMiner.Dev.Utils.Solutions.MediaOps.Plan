namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    internal partial class ConfigurationInstance
    {
        protected override void BeforeToInstance()
        {
            foreach (var section in OrchestrationEvents)
            {
                section.ApplyChanges();
            }
        }
    }
}
