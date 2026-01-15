namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an orchestration event.
    /// </summary>
    public class OrchestrationEvent : TrackableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrchestrationEvent"/> class.
        /// </summary>
        public OrchestrationEvent()
        {
            IsNew = true;
        }

        internal OrchestrationEvent(OrchestrationEvent orchestrationEvent)
        {
            EventType = orchestrationEvent.EventType;
            ExecutionDetails = orchestrationEvent.ExecutionDetails;
            Metadata = orchestrationEvent.Metadata;

            IsNew = orchestrationEvent.IsNew;
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the type of the orchestration event.
        /// </summary>
        public OrchestrationEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the details of the script execution associated with this orchestration event.
        /// </summary>
        public ScriptExecutionDetails ExecutionDetails { get; set; }

        /// <summary>
		/// Gets or sets the metadata associated with this orchestration event.
		/// </summary>
		public string Metadata { get; set; }

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }

        internal static string TranslateEventType(OrchestrationEventType eventType)
        {
            return Convert.ToString(eventType);
        }

        internal static OrchestrationEventType TranslateEventType(string eventType)
        {
            switch (eventType.ToLower())
            {
                case "prerollstart": return OrchestrationEventType.PrerollStart;
                case "prerollstop": return OrchestrationEventType.PrerollStop;
                case "postrollstart": return OrchestrationEventType.PostrollStart;
                case "postrollstop": return OrchestrationEventType.PostrollStop;

                default:
                    throw new ArgumentException($"Unknown event type: {eventType}");
            }
        }
    }
}
