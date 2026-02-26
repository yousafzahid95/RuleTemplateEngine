using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.TemplateEngine
{
    /// <summary>
    /// Resolves bracket expressions like [LEM.EntityId] or [LEM[2].EntityId]
    /// using a dataset keyed by DataSourceKey (e.g. "LEM", "InfoRequest"). Each key maps to an IEnumerable of IDataRecord from that source.
    /// </summary>
    internal static class ExpressionResolver
    {
        private static readonly char[] Dot = { '.' };

        /// <summary>
        /// Resolves a single expression e.g. "[LEM.EntityId]" or "[LEM[2].Id]".
        /// Dataset is keyed by DataSourceKey; [DataSourceKey.Property] uses the first record, [DataSourceKey[i].Property] uses the i-th (0-based).
        /// </summary>
        public static object? Resolve(string expression, IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> dataset)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;

            var trimmed = expression.Trim();
            if (trimmed.Length < 3 || trimmed[0] != '[' || trimmed[^1] != ']')
                return trimmed;

            var path = trimmed[1..^1].Trim();
            if (string.IsNullOrEmpty(path))
                return null;

            var parts = path.Split(Dot, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return null;

            var (dataSourceKey, index) = ParseSourceSegment(parts[0]);
            var propertyPath = string.Join(".", parts.Skip(1));

            if (!dataset.TryGetValue(dataSourceKey, out var records) || records == null || records.Count == 0)
                return null;

            if (index < 0 || index >= records.Count)
                return null;

            var record = records[index];
            // Support both prefixed (e.g. CustomDataRecord with "LEM") and non-prefixed column names
            var prefixedPath = $"{dataSourceKey}.{propertyPath}";
            var value = record[prefixedPath];
            if (value != null)
                return value;
            return record[propertyPath];
        }

        private static (string dataSourceKey, int index) ParseSourceSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return (segment, 0);

            var bracket = segment.IndexOf('[');
            if (bracket < 0)
                return (segment, 0);

            var dataSourceKey = segment[0..bracket];
            var closeBracket = segment.IndexOf(']', bracket);
            if (closeBracket < 0)
                return (segment, 0);

            var indexStr = segment[(bracket + 1)..closeBracket];
            if (!int.TryParse(indexStr, out var index) || index < 0)
                return (dataSourceKey, 0);

            return (dataSourceKey, index);
        }
    }
}
