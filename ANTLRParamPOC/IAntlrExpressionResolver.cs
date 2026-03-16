namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Internal interface for resolving ANTLR expressions.
    /// Used by <see cref="IAntlrParamResolver"/>.
    /// </summary>
    public interface IAntlrExpressionResolver
    {
        object? Resolve(string expression, EvaluationContext context);
    }
}
