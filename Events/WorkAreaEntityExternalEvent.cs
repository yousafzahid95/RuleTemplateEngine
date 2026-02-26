namespace RuleTemplateEngine.Events
{
    public class WorkAreaEntityExternalEvent
    {
        public Guid ProjectId { get; set; }
        public Guid WorkAreaId { get; set; }
        public Guid Id { get; set; }
    }
}
