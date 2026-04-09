namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	internal partial class OrchestrationEventsSection
	{
		internal ScriptExecutionDetails ScriptExecutionDetails { get; set; }

		protected override void BeforeToSection()
		{
			if (ScriptExecutionDetails != null)
			{
				ScriptInputValues = ScriptExecutionDetails.Serialize();
			}
			else
			{
				ScriptInputValues = null;
			}
		}

		protected override void AfterLoad()
		{
			if (!string.IsNullOrEmpty(ScriptInputValues) && ScriptExecutionDetails.TryDeserialize(ScriptInputValues, out var details))
			{
				ScriptExecutionDetails = details;
			}
			else
			{
				ScriptExecutionDetails = null;
			}
		}
	}
}
