using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateEngine
{
    /// <summary>
    /// Resolves a TemplateParam against a dataset keyed by DataSourceKey.
    /// Any key present in the dataset is supported: LEM, EventMessage, EventData, Event, or custom keys.
    /// Uses String.Format and bracket expressions like [LEM.Property], [EventMessage.MessageId], [EventData.ProjectId], or [SourceKey[index].Property].
    /// </summary>
    public static class RuleTemplateEngine
    {
        /// <summary>
        /// Resolves a TemplateParam by resolving each param expression against the dataset,
        /// then calling String.Format(template, ...values).
        /// params[0] -> {0}, params[1] -> {1}, etc.
        /// Special case: template "{0}" with multiple params uses first non-empty (fallback).
        /// </summary>
        public static string Resolve(TemplateParam param, IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> dataset)
        {
            if (param == null || string.IsNullOrEmpty(param.Template))
                return string.Empty;

            if (param.Params == null || param.Params.Count == 0)
                return param.Template;

            var rawValues = new object?[param.Params.Count];
            for (var i = 0; i < param.Params.Count; i++)
                rawValues[i] = ExpressionResolver.Resolve(param.Params[i], dataset);

            // Special case: single placeholder with multiple params -> first non-empty fallback
            if (string.Equals(param.Template, "{0}", StringComparison.Ordinal) && param.Params.Count > 1)
            {
                var firstNonEmpty = rawValues.FirstOrDefault(v =>
                    v != null && !string.IsNullOrWhiteSpace(v?.ToString()));
                if (firstNonEmpty == null)
                    return string.Empty;
                return string.Format(param.Template, firstNonEmpty);
            }

            return FormatSafely(param.Template, rawValues);
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
