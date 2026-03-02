using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateEngine
{
    /// <summary>
    /// Resolves a TemplateParam against a dataset (list of records). Keys are derived from each record's
    /// column names (e.g. CustomDataRecord built with prefix "LEM" has columns "LEM.EntityId") — no position-based keying.
    /// Uses String.Format and bracket expressions like [LEM.Property], [Event.ProjectId], or [LEM[index].Property].
    /// </summary>
    public static class RuleTemplateEngine
    {
        /// <summary>
        /// Resolves a TemplateParam (2D params) against the dataset.
        /// Params[i] is a list of fallback expressions for placeholder {i}.
        /// For each placeholder, expressions are tried in order; first non-empty value wins.
        /// </summary>
        /// <param name="param">Template and 2D param expressions.</param>
        /// <param name="dataset">List of records. Each record's key (e.g. LEM, Event) is taken from its column names (prefix used in CustomDataRecord transformation).</param>
        public static string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset)
        {
            if (param == null || string.IsNullOrEmpty(param.Template))
                return string.Empty;

            if (param.Params == null || param.Params.Count == 0)
                return param.Template;

            var keyed = BuildKeyedDataset(dataset);
            var rawValues = new object?[param.Params.Count];

            for (var i = 0; i < param.Params.Count; i++)
            {
                var candidates = param.Params[i];
                if (candidates == null || candidates.Count == 0)
                {
                    rawValues[i] = null;
                    continue;
                }

                // Try each candidate expression; first non-empty wins
                foreach (var expr in candidates)
                {
                    var resolved = ExpressionResolver.Resolve(expr, keyed);
                    if (resolved != null && !string.IsNullOrWhiteSpace(resolved.ToString()))
                    {
                        rawValues[i] = resolved;
                        break;
                    }
                }
            }

            return FormatSafely(param.Template, rawValues);
        }

        /// <summary>
        /// Builds keyed dataset by grouping records by the key already in their column names.
        /// E.g. a record with columns "LEM.EntityId", "LEM.WorkAreaId" is grouped under "LEM"; "[Event.ProjectId]" under "Event".
        /// </summary>
        internal static IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> BuildKeyedDataset(IReadOnlyList<IDataRecord> datasetList)
        {
            var keyed = new Dictionary<string, IReadOnlyList<IDataRecord>>(StringComparer.OrdinalIgnoreCase);
            if (datasetList == null || datasetList.Count == 0) return keyed;

            foreach (var record in datasetList)
            {
                var key = GetDataSourceKeyFromRecord(record);
                if (string.IsNullOrEmpty(key))
                    continue;
                if (!keyed.TryGetValue(key, out var list))
                {
                    list = new List<IDataRecord>();
                    keyed[key] = list;
                }
                ((List<IDataRecord>)list).Add(record);
            }
            return keyed;
        }

        /// <summary>
        /// Infers the data source key from a record's columns (first segment before '.' e.g. "LEM.EntityId" -> "LEM").
        /// </summary>
        private static string? GetDataSourceKeyFromRecord(IDataRecord record)
        {
            if (record?.Columns == null || record.Columns.Length == 0)
                return null;
            foreach (var col in record.Columns)
            {
                var dot = col.IndexOf('.');
                if (dot > 0)
                    return col[..dot];
            }
            return null;
        }

        private static string FormatSafely(string template, IReadOnlyList<object?> values)
        {
            var args = values
                .Select(v => (object?)(v ?? string.Empty))
                .ToArray();

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                // If the template and values don't line up, fall back to the template literal.
                return template;
            }
        }
    }
}
