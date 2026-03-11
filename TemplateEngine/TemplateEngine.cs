using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateEngine
{
    /// <summary>
    /// Resolves a TemplateParam against a dataset (list of records).
    /// Keys are derived from each record's column names
    /// (e.g. CustomDataRecord built with prefix "LEM" has columns "LEM.EntityId").
    /// Uses String.Format and bracket expressions like [LEM.Property], [Event.ProjectId], or [LEM[index].Property].
    /// </summary>
    public static class RuleTemplateEngine
    {
        /// <summary>
        /// Resolves a TemplateParam by resolving each param expression against the dataset,
        /// then calling String.Format(template, ...values).
        /// params[0] -> {0}, params[1] -> {1}, etc.
        /// Special case: template "{0}" with multiple params uses first non-empty (fallback).
        /// </summary>
        /// <param name="param">Template and param expressions.</param>
        /// <param name="dataset">
        /// List of records. Each record's key (e.g. LEM, Event) is taken from its column names
        /// (prefix used in CustomDataRecord transformation).
        /// </param>
        public static string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset)
        {
            if (param == null || string.IsNullOrEmpty(param.Template))
            {
                return string.Empty;
            }

            if (param.Params == null || param.Params.Count == 0)
            {
                return param.Template;
            }

            var keyed = BuildKeyedDataset(dataset);
            var rawValues = new object?[param.Params.Count];

            for (var i = 0; i < param.Params.Count; i++)
            {
                var currentParam = param.Params[i];

                rawValues[i] = IsExpression(currentParam)
                    ? ExpressionResolver.Resolve(currentParam, keyed)
                    : currentParam;
            }

            // Special case: single placeholder with multiple params -> first non-empty fallback
            if (string.Equals(param.Template, "{0}", StringComparison.Ordinal) && param.Params.Count > 1)
            {
                var firstNonEmpty = rawValues.FirstOrDefault(v =>
                    v != null && !string.IsNullOrWhiteSpace(v.ToString()));

                if (firstNonEmpty == null)
                {
                    return string.Empty;
                }

                return string.Format(param.Template, firstNonEmpty);
            }

            return FormatSafely(param.Template, rawValues);
        }

        /// <summary>
        /// Builds keyed dataset by grouping records by the key already in their column names.
        /// E.g. a record with columns "LEM.EntityId", "LEM.WorkAreaId" is grouped under "LEM";
        /// a record with "Event.ProjectId" is grouped under "Event".
        /// </summary>
        internal static IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> BuildKeyedDataset(IReadOnlyList<IDataRecord> datasetList)
        {
            var keyed = new Dictionary<string, IReadOnlyList<IDataRecord>>(StringComparer.OrdinalIgnoreCase);

            if (datasetList == null || datasetList.Count == 0)
            {
                return keyed;
            }

            foreach (var record in datasetList)
            {
                var key = GetDataSourceKeyFromRecord(record);
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

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
        /// Infers the data source key from a record's columns
        /// (first segment before '.' e.g. "LEM.EntityId" -> "LEM").
        /// </summary>
        private static string? GetDataSourceKeyFromRecord(IDataRecord record)
        {
            if (record?.Columns == null || record.Columns.Length == 0)
            {
                return null;
            }

            foreach (var col in record.Columns)
            {
                var dot = col.IndexOf('.');
                if (dot > 0)
                {
                    return col[..dot];
                }
            }

            return null;
        }

        private static bool IsExpression(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            return trimmed.Length >= 3
                   && trimmed[0] == '['
                   && trimmed[^1] == ']';
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
