namespace RuleTemplateEngine.Events
{
    public enum CloseReason
    {
        Unknown = 0,
        Completed = 1,
        Cancelled = 2,
        Duplicate = 3
    }

    public enum WorkplanTaskStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Blocked = 3
    }
}
