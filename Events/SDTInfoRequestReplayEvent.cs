namespace RuleTemplateEngine.Events
{
    public class SDTInfoRequestReplayEvent
    {
        public InfoRequestReplayData InfoRequest { get; set; } = new();
    }

    public class InfoRequestReplayData
    {
        public Guid ProjectId { get; set; }
        public Guid WorkareaId { get; set; }
        public List<Guid> EntityIds { get; set; } = new();
    }
}
