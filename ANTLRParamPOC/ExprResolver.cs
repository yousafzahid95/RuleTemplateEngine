using System.Collections.Concurrent;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public class ExpressionCache
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

    public class EvaluationContext
    {
        private readonly IList<IDataRecord> _records;

        public EvaluationContext(IList<IDataRecord> records)
        {
            _records = records;
        }

        public object? Resolve(string fullPath)
        {
            var parts = fullPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return null;

            var (rootKey, index) = ParseSourceSegment(parts[0]);
            var pathArray = parts.Skip(1).ToArray();

            return ResolveFromDataset(rootKey, index, pathArray);
        }

        private static (string key, int index) ParseSourceSegment(string segment)
        {
            var bracket = segment.IndexOf('[');
            if (bracket < 0)
                return (segment, 0);

            var key = segment.Substring(0, bracket);
            var closeBracket = segment.IndexOf(']', bracket);

            if (closeBracket > bracket)
            {
                if (int.TryParse(segment.Substring(bracket + 1, closeBracket - bracket - 1), out var idx))
                {
                    return (key, idx);
                }
            }

            return (key, 0);
        }

        private object? ResolveFromDataset(string dataSourceKey, int index, string[] path)
        {
            // filter records belonging to the datasource
            var datasetRecords = _records
                .Where(r => r.Columns.Any(c => c.StartsWith(dataSourceKey + ".", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (datasetRecords.Count == 0)
                return null;

            if (index < 0 || index >= datasetRecords.Count)
                return null;

            if (path.Length == 0)
                return datasetRecords[index];

            var record = datasetRecords[index];

            var fullKey = dataSourceKey + "." + string.Join(".", path);

            var value = record[fullKey];
            if (value != null)
                return value;

            return record[string.Join(".", path)];
        }
    }

    public class RuleTemplateVisitor : RuleTemplateParserBaseVisitor<object?>
    {
        private readonly EvaluationContext _context;

        public RuleTemplateVisitor(EvaluationContext context)
        {
            _context = context;
        }

        // Concatenates all parts into final string
        public override object? VisitTemplate(RuleTemplateParser.TemplateContext ctx)
        {
            var sb = new StringBuilder();
            foreach (var part in ctx.templatePart())
                sb.Append(Visit(part));
            return sb.ToString();
        }

        // Plain text → return as-is
        public override object? VisitLiteralPart(RuleTemplateParser.LiteralPartContext ctx)
            => ctx.TEXT().GetText();

        public override object? VisitLiteralSlashPart(RuleTemplateParser.LiteralSlashPartContext ctx)
            => ctx.ANY_OTHER_SLASH().GetText();

        public override object? VisitEscapedBrace(RuleTemplateParser.EscapedBraceContext ctx)
            => "{";

        // {AllWorkplan.RootTaskId} → call context
        public override object? VisitInterpolationPart(RuleTemplateParser.InterpolationPartContext ctx)
        {
            var expression = ctx.expression();
            if (expression != null)
            {
                return Visit(expression);
            }
            return null;
        }

        public override object? VisitAccessorExpr(RuleTemplateParser.AccessorExprContext ctx)
        {
            var accessor = ctx.accessor();
            return VisitAccessorNode(accessor);
        }

        // {A.X ?? B.Y} → try each accessor, return first non-null
        public override object? VisitNullCoalesceExpr(RuleTemplateParser.NullCoalesceExprContext ctx)
        {
            foreach (var accessor in ctx.accessor())
            {
                var result = VisitAccessorNode(accessor);
                if (result is not null) return result;
            }
            return null;
        }

        private object? VisitAccessorNode(RuleTemplateParser.AccessorContext accessor)
        {
            var text = accessor.GetText();
            if (string.IsNullOrWhiteSpace(text)) return null;

            return _context.Resolve(text);
        }
    }

    public class ExpressionResolver
    {
        private readonly ExpressionCache _cache;

        public ExpressionResolver(ExpressionCache cache)
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
