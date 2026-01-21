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

        /// <summary>
        /// Checks if the provided object is an OrchestrationEvent instance and compares its properties to determine equality.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True, if properties match, else false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not OrchestrationEvent orchestrationEvent)
            {
                return false;
            }

            return EventType == orchestrationEvent.EventType &&
                Object.Equals(ExecutionDetails, orchestrationEvent.ExecutionDetails) &&
                Metadata == orchestrationEvent.Metadata &&
                OriginalSection?.ID == orchestrationEvent.OriginalSection?.ID;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + EventType.GetHashCode();
                hash = (hash * 23) + (ExecutionDetails != null ? ExecutionDetails.GetHashCode() : 0);
                hash = (hash * 23) + (Metadata != null ? Metadata.GetHashCode() : 0);
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);

                return hash;
            }
        }

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
