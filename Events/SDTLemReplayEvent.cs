namespace RuleTemplateEngine.Events
{
    public class SDTLemReplayEvent
    {
        public LemEventData LemEvent { get; set; } = new();
    }

    public class LemEventData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkareaId { get; set; }
        public Guid EntityId { get; set; }
    }
}
