using System;
using System.Collections.Generic;
using System.Linq;
using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Default implementation of <see cref="IAntlrParamResolver"/> that uses ExpressionResolver.
    /// This is the DI-friendly entry point for the ANTLR-based expression engine.
    /// </summary>
    public class AntlrParamResolver : IAntlrParamResolver
    {
        private readonly IAntlrExpressionResolver _resolver;

        public AntlrParamResolver(IAntlrExpressionResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public string Resolve(string expression, IReadOnlyList<IDataRecord> dataset)
        {
            var context = new EvaluationContext(dataset.ToList());
            var result = _resolver.Resolve(expression, context);
            return result?.ToString() ?? string.Empty;
        }
    }
}

