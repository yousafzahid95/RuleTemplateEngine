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

        // {A.X ?? B.Y} 
        public override object? VisitNullCoalesceExpr(RuleTemplateParser.NullCoalesceExprContext ctx)
        {
            var left = Visit(ctx.expression(0));
            if (left is not null) return left;
            return Visit(ctx.expression(1));
        }

        public override object? VisitEqualityExpr(RuleTemplateParser.EqualityExprContext ctx)
        {
            var left = Visit(ctx.expression(0));
            var right = Visit(ctx.expression(1));
            return string.Equals(left?.ToString(), right?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public override object? VisitLogicalOrExpr(RuleTemplateParser.LogicalOrExprContext ctx)
        {
            var left = Visit(ctx.expression(0));
            if (left is bool bLeft && bLeft) return true;
            return Visit(ctx.expression(1));
        }

        public override object? VisitTernaryExpr(RuleTemplateParser.TernaryExprContext ctx)
        {
            var condition = Visit(ctx.expression(0));
            if (condition is bool bCond && bCond)
                return Visit(ctx.expression(1));
            return Visit(ctx.expression(2));
        }

        public override object? VisitParenthesizedExpr(RuleTemplateParser.ParenthesizedExprContext ctx)
            => Visit(ctx.expression());

        public override object? VisitStringLiteralExpr(RuleTemplateParser.StringLiteralExprContext ctx)
        {
            var text = ctx.GetText();
            return text.Substring(1, text.Length - 2); // strip quotes
        }

        private object? VisitAccessorNode(RuleTemplateParser.AccessorContext accessor)
        {
            var text = accessor.GetText();
            if (string.IsNullOrWhiteSpace(text)) return null;

            return _context.Resolve(text);
        }
    }
}
