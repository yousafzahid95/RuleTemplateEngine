using System.Collections.Concurrent;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Thread-safe implementation of <see cref="IExpressionCache"/> using ConcurrentDictionary.
    /// </summary>
    public class ExpressionCache : IExpressionCache
    {
        private readonly ConcurrentDictionary<string, IParseTree> _cache = new();

        public IParseTree GetOrParse(string expression)
        {
            return _cache.GetOrAdd(expression, expr =>
            {
                var inputStream  = new AntlrInputStream(expr);
                var lexer        = new RuleTemplateLexer(inputStream);
                var tokenStream  = new CommonTokenStream(lexer);
                var parser       = new RuleTemplateParser(tokenStream);
                return parser.template(); // returns ParseTree root
            });
        }
    }
}
