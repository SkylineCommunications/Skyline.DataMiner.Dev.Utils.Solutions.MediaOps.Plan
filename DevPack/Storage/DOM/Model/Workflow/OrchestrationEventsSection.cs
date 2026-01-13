namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
    internal partial class OrchestrationEventsSection
    {
        private ScriptExecutionDetails scriptExecutionDetails;

        internal ScriptExecutionDetails ScriptExecutionDetails
        {
            get
            {
                if (scriptExecutionDetails != null)
                {
                    return scriptExecutionDetails;
                }

                if (ScriptExecutionDetails.TryDeserialize(ScriptInputValues, out scriptExecutionDetails))
                {
                    return scriptExecutionDetails;
                }

                scriptExecutionDetails = null;
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
                ScriptInputValues = scriptExecutionDetails.Serialize();
            }
        }
    }
}
