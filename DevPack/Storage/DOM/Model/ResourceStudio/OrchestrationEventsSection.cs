namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	internal partial class OrchestrationEventsSection
	{
		internal ScriptExecutionDetails ScriptExecutionDetails { get; set; }

		protected override void BeforeToSection()
		{
			if (ScriptExecutionDetails != null)
			{
				ScriptInput = ScriptExecutionDetails.Serialize();
			}
			else
			{
				ScriptInput = null;
			}
		}

		protected override void AfterLoad()
		{
			if (!String.IsNullOrEmpty(ScriptInput) && ScriptExecutionDetails.TryDeserialize(ScriptInput, out var details))
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
