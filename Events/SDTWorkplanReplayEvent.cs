namespace RuleTemplateEngine.Events
{
    public class SDTWorkplanReplayEvent
    {
        public WorkplanReplayData WorkplanTask { get; set; } = new();
    }

    public class WorkplanReplayData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkareaId { get; set; }
        public List<Guid> EntityIds { get; set; } = new();
    }
}
