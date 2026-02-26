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
        /// Params is interpreted as grouped fallbacks: Params[0] -> {0}, Params[1] -> {1}, etc.
        /// Each group is evaluated with first-non-empty semantics.
        /// </summary>
        public static string Resolve(TemplateParam param, IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> dataset)
        {
            if (param == null || string.IsNullOrEmpty(param.Template))
                return string.Empty;

            if (param.Params == null || param.Params.Count == 0)
                return param.Template;

            var slotValues = new List<object?>(param.Params.Count);

            foreach (var group in param.Params)
            {
                object? chosen = null;

                if (group != null)
                {
                    foreach (var expr in group)
                    {
                        if (string.IsNullOrWhiteSpace(expr))
                            continue;

                        var value = ExpressionResolver.Resolve(expr, dataset);
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                        {
                            chosen = value;
                            break;
                        }
                    }
                }

                slotValues.Add(chosen);
            }

            return FormatSafely(param.Template, slotValues);
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
