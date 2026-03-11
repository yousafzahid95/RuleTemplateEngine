namespace RuleTemplateEngine.Events
{
    public class ActionItemManualClosedEvent
    {
        public ActionItemClosedData ActionItemClosedEvent { get; set; } = new();
    }

    public class ActionItemClosedData
    {
        public Guid WorkAreaId { get; set; }
        public Guid EntityId { get; set; }
        public CloseReason? Reason { get; set; }
    }
}
