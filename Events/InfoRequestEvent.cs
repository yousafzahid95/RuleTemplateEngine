namespace RuleTemplateEngine.Events
{
    public class InfoRequestEvent
    {
        public InfoRequestData InfoRequest { get; set; } = new();
    }

    public class InfoRequestData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkareaId { get; set; }
        public List<Guid> EntityIds { get; set; } = new();
    }
}
