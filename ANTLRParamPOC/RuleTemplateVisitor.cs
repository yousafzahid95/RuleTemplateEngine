using System.Text;
using Antlr4.Runtime.Misc;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    /// <summary>
    /// Visitor implementation that traverses the ANTLR parse tree to evaluate the template.
    /// </summary>
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
}
