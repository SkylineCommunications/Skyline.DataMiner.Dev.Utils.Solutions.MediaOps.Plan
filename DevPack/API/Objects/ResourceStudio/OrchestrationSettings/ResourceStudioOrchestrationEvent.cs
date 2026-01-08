namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioOrchestrationEvent : OrchestrationEvent
    {
        internal StorageResourceStudio.OrchestrationEventsSection originalSection;
        internal StorageResourceStudio.OrchestrationEventsSection updatedSection;

        internal ResourceStudioOrchestrationEvent(OrchestrationEvent orchestrationEvent) : base(orchestrationEvent)
        {
        }

        internal ResourceStudioOrchestrationEvent(MediaOpsPlanApi planApi, StorageResourceStudio.OrchestrationEventsSection section)
        {
            ParseSection(planApi, section);
            InitTracking();
        }

        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.OrchestrationEventsSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.OrchestrationEventsSection() : originalSection.Clone();
            }

            updatedSection.Name = TranslateEventType(EventType);

            if (ExecutionDetails != null)
            {
                updatedSection.Script = ExecutionDetails.ScriptName;
                updatedSection.ScriptExecutionDetails = ExecutionDetails.ToStorage();
            }
            else
            {
                updatedSection.Script = null;
                updatedSection.ScriptInput = null;
            }

            return updatedSection;
        }

        private void ParseSection(MediaOpsPlanApi planApi, StorageResourceStudio.OrchestrationEventsSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            EventType = TranslateEventType(section.Name);
            if (section.ScriptExecutionDetails != null)
            {
                ExecutionDetails = ScriptExecutionDetails.FromStorage(planApi, section.ScriptExecutionDetails);
            }
        }
    }
}
