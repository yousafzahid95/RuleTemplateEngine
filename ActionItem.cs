namespace RuleTemplateEngine
{
    /// <summary>
    /// Represents an action item created from extracted parameters and dataset values.
    /// </summary>
    public class ActionItem
    {
        public Guid EntityId { get; set; }
        public Guid WorkAreaId { get; set; }
        public Guid TaskId { get; set; }

        /// <summary>
        /// External key used by the source system, e.g. A002IR_{EntityId}.
        /// </summary>
        public string SourceSystemKey { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable description, typically built from templates.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Status of the action item: "Open" or "Closed".
        /// </summary>
        public string Status { get; set; } = "Open";
    }
}

