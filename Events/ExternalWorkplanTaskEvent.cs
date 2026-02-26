namespace RuleTemplateEngine.Events
{
    public class ExternalWorkplanTaskEvent
    {
        public WorkplanTaskData WorkplanTask { get; set; } = new();
    }

    public class WorkplanTaskData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkAreaId { get; set; }
        public Guid TaskId { get; set; }
        public Guid EntityId { get; set; }
        public Guid? ParentId { get; set; }
        public List<WorkplanTaskEntity>? Entities { get; set; }
    }

    public class WorkplanTaskEntity
    {
        public Guid WorkAreaEntityId { get; set; }
    }
}
