using System.Collections.Generic;
using RuleTemplateEngine.Interfaces;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public interface IAntlrParamResolver
    {
        string Resolve(string expression, IReadOnlyList<IDataRecord> dataset);
    }
}
