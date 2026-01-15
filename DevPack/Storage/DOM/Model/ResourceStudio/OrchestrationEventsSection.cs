namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
    internal partial class OrchestrationEventsSection
    {
        private ScriptExecutionDetails scriptExecutionDetails;
        private bool isScriptExecutionDetailsLoaded = false;

        internal ScriptExecutionDetails ScriptExecutionDetails
        {
            get
            {
                if (!isScriptExecutionDetailsLoaded)
                {
                    ScriptExecutionDetails.TryDeserialize(ScriptInput, out scriptExecutionDetails);
                    isScriptExecutionDetailsLoaded = true;
                }


                return scriptExecutionDetails;
            }
            set
            {
                scriptExecutionDetails = value;
            }
        }

        internal void ApplyChanges()
        {
            if (scriptExecutionDetails != null)
            {
                ScriptInput = scriptExecutionDetails.Serialize();
            }
            else
            {
                ScriptInput = null;
            }
        }
    }
}
