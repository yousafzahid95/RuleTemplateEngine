using System.Collections.Generic;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.TemplateEngine
{
    public interface ITemplateParamResolver
    {
        string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset);
    }
}
