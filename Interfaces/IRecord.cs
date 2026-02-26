namespace RuleTemplateEngine.Interfaces
{
    /// <summary>
    /// Record with indexer access by column name (e.g. CustomDataRecord, in-memory records).
    /// </summary>
    public interface IRecord
    {
        object? this[string columnName] { get; }
    }
}
