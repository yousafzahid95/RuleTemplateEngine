using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateEngine
{
    public interface ITemplateParamResolver
    {
        string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset);
    }
}

