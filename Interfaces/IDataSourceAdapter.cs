using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.Interfaces
{
    /// <summary>
    /// Data source adapter: resolves params from the rule and returns records (e.g. LEM) from the external source.
    /// </summary>
    public interface IDataSourceAdapter
    {
        /// <summary>
        /// Fetches records for the given event and data source parameters.
        /// </summary>
        /// <param name="eventData">The incoming event.</param>
        /// <param name="dataSourceParams">Template-based params from the rule (e.g. ProjectId, WorkAreaId, EntityId with expressions like [Event.ProjectId]).</param>
        /// <param name="dataset">Records from the event/source used to resolve param expressions (e.g. first record used for [Event.ProjectId]).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Records from the data source (e.g. LEM).</returns>
        Task<IEnumerable<IDataRecord>> GetRecordsAsync(
            object eventData,
            IDictionary<string, TemplateParam> dataSourceParams,
            IReadOnlyList<IDataRecord> dataset,
            CancellationToken cancellationToken = default);
    }
}
