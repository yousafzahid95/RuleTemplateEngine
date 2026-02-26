namespace RuleTemplateEngine.Interfaces
{
    /// <summary>
    /// Data record with Id, Columns, and indexer (matches your domain IDataRecord).
    /// Records are grouped by DataSourceKey (e.g. "LEM") in the dataset; the key is not on the record.
    /// </summary>
    public interface IDataRecord : IRecord
    {
        string Id { get; }
        string[] Columns { get; }
    }
}
