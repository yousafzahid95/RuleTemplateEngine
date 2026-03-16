using Antlr4.Runtime.Tree;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Interface for caching ANTLR parse trees to avoid redundant parsing overhead.
    /// </summary>
    public interface IExpressionCache
    {
        IParseTree GetOrParse(string expression);
    }
}
