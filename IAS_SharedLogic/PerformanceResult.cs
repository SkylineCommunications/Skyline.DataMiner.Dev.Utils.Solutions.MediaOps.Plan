namespace IAS_SharedLogic
{
    internal class PerformanceResult
    {
        public int BatchSize { get; set; }

        public long ResourceCreationDuration { get; set; }

        public long ResourceMoveToCompleteDuration { get; set; }

        public long ResourceDeletionDuration { get; set; }
    }
}
