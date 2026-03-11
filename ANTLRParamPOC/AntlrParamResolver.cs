using System;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Default implementation of <see cref="IAntlrParamResolver"/> that uses ExpressionResolver.
    /// This is the DI-friendly entry point for the ANTLR-based expression engine.
    /// </summary>
    public class AntlrParamResolver : IAntlrParamResolver
    {
        private readonly ExpressionResolver _resolver;

        public AntlrParamResolver(ExpressionResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public string Resolve(string template, EvaluationContext context)
        {
            var result = _resolver.Resolve(template, context);
            return result?.ToString() ?? string.Empty;
        }
    }
}

