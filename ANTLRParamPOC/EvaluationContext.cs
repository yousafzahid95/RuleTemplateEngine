using System;
using System.Collections.Generic;
using System.Linq;
using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Provides the data context for resolving expressions against a dataset of IDataRecords.
    /// </summary>
    public class EvaluationContext
    {
        private readonly IList<IDataRecord> _records;

        public EvaluationContext(IList<IDataRecord> records)
        {
            _records = records;
        }

        public object? Resolve(string fullPath)
        {
            var parts = fullPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            var (rootKey, index) = ParseSourceSegment(parts[0]);
            var pathArray = parts.Skip(1).ToArray();

            return ResolveFromDataset(rootKey, index, pathArray);
        }

        private static (string key, int index) ParseSourceSegment(string segment)
        {
            var bracket = segment.IndexOf('[');
            if (bracket < 0)
                return (segment, 0);

            var key = segment.Substring(0, bracket);
            var closeBracket = segment.IndexOf(']', bracket);

            if (closeBracket > bracket)
            {
                if (int.TryParse(segment.Substring(bracket + 1, closeBracket - bracket - 1), out var idx))
                {
                    return (key, idx);
                }
            }

            return (key, 0);
        }

        private object? ResolveFromDataset(string dataSourceKey, int index, string[] path)
        {
            // filter records belonging to the datasource
            var datasetRecords = _records
                .Where(r => r.Columns.Any(c => c.StartsWith(dataSourceKey + ".", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (datasetRecords.Count == 0)
                return null;

            if (index < 0 || index >= datasetRecords.Count)
                return null;

            if (path.Length == 0)
                return datasetRecords[index];

            var record = datasetRecords[index];

            var fullKey = dataSourceKey + "." + string.Join(".", path);

            var value = record[fullKey];
            if (value != null)
                return value;

            return record[string.Join(".", path)];
        }
    }
}
