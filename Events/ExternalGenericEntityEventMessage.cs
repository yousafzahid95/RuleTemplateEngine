namespace RuleTemplateEngine.Events
{
    public interface IMessageHeaders
    {
        Guid MessageId { get; set; }
        object? Body { get; set; }
        string Label { get; set; }
        DateTime Timestamp { get; set; }
        string EventType { get; set; }
        MessageHeadersFields? MessageHeaders { get; set; }
    }

    public class MessageHeadersFields
    {
        public string? CorrelationId { get; set; }
        public string? ReplyTo { get; set; }
    }

    public abstract class BaseExternalMessage : IMessageHeaders
    {
        public Guid MessageId { get; set; }
        public object? Body { get; set; }
        public string Label { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public virtual string EventType { get; set; } = string.Empty;
        public MessageHeadersFields? MessageHeaders { get; set; }
    }

    public class ExternalGenericEntityEventMessage : BaseExternalMessage
    {
    }
}
