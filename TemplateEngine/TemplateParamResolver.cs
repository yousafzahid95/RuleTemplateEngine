using System;
using System.Collections.Generic;
using System.Linq;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateEngine
{
    /// <summary>
    /// DI-friendly implementation of <see cref="ITemplateParamResolver"/>.
    /// This class contains the non-static logic previously found on the static RuleTemplateEngine.
    /// </summary>
    public class TemplateParamResolver : ITemplateParamResolver
    {
        private readonly IExpressionResolver _expressionResolver;

        public TemplateParamResolver(IExpressionResolver expressionResolver)
        {
            _expressionResolver = expressionResolver ?? throw new ArgumentNullException(nameof(expressionResolver));
        }

        public string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset)
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
                    ? _expressionResolver.Resolve(currentParam, keyed)
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

        public IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> BuildKeyedDataset(IReadOnlyList<IDataRecord> datasetList)
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

        private string? GetDataSourceKeyFromRecord(IDataRecord record)
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

        private bool IsExpression(string? value)
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

        private string FormatSafely(string template, IReadOnlyList<object?> values)
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

