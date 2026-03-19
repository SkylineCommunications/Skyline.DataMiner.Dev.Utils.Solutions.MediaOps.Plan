namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	internal class WorkflowOrchestrationEvent : OrchestrationEvent
	{
		private StorageWorkflow.OrchestrationEventsSection originalSection;
		private StorageWorkflow.OrchestrationEventsSection updatedSection;

		internal WorkflowOrchestrationEvent(OrchestrationEvent orchestrationEvent) : base(orchestrationEvent)
		{
		}

		internal WorkflowOrchestrationEvent(MediaOpsPlanApi planApi, StorageWorkflow.OrchestrationEventsSection section)
		{
			ParseSection(planApi, section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageWorkflow.OrchestrationEventsSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageWorkflow.OrchestrationEventsSection() : originalSection.Clone();
			}

			updatedSection.Name = TranslateEventType(EventType);
			updatedSection.Metadata = Metadata;

			if (ExecutionDetails != null)
			{
				updatedSection.Script = ExecutionDetails.ScriptName;
				updatedSection.ScriptExecutionDetails = ExecutionDetails.ToStorage();
			}
			else
			{
				updatedSection.Script = null;
				updatedSection.ScriptExecutionDetails = null;
			}

			return updatedSection;
		}

		private void ParseSection(MediaOpsPlanApi planApi, StorageWorkflow.OrchestrationEventsSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			EventType = TranslateEventType(section.Name);
			Metadata = section.Metadata;

			if (section.ScriptExecutionDetails != null)
			{
				ExecutionDetails = ScriptExecutionDetails.FromStorage(planApi, section.ScriptExecutionDetails);
			}
		}
	}
}
