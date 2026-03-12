namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Implementation of <see cref="IAntlrExpressionResolver"/> that orchestrates the cache and visitor.
    /// </summary>
    public class ExpressionResolver : IAntlrExpressionResolver
    {
        private readonly IExpressionCache _cache;

        public ExpressionResolver(IExpressionCache cache)
        {
            _cache = cache;
        }

        public object? Resolve(string expression, EvaluationContext context)
        {
            var tree    = _cache.GetOrParse(expression);
            var visitor = new RuleTemplateVisitor(context);
            return visitor.Visit(tree);
        }
    }
}
