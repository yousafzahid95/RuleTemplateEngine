namespace RuleTemplateEngine.Events
{
    public class ExternalMemberRelationEventMessage
    {
        public string Body { get; set; } = string.Empty;
    }

    public class MemberRelationExternalEvent
    {
        public Guid WorkareaId { get; set; }
        public Guid EntityId { get; set; }
    }
}
