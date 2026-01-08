namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents an orchestration event.
    /// </summary>
    public class OrchestrationEvent : TrackableObject
    {
        /// <summary>
        /// Gets or sets the type of the orchestration event.
        /// </summary>
        public OrchestrationEventType EventType { get; set; }

        /// <summary>
        /// Gets or sets the details of the script execution associated with this orchestration event.
        /// </summary>
        public ScriptExecutionDetails ScriptExecutionDetails { get; set; }

        /// <summary>
		/// Gets or sets the metadata associated with this orchestration event.
		/// </summary>
		public string Metadata { get; set; }

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
    }
}
