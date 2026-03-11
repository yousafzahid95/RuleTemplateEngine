namespace RuleTemplateEngine.ANTLRParamPOC
{
    public interface IAntlrParamResolver
    {
        string Resolve(string template, EvaluationContext context);
    }
}

