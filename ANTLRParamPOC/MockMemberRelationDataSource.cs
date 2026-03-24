using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public class MockMemberRelationDataSource : IDataSourceAdapter
    {
        public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
            object eventData,
            IDictionary<string, TemplateParam> dataSourceParams,
            IReadOnlyList<IDataRecord> dataset, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Not implemented for legacy template flow in POC");
        }

        public async Task<IEnumerable<IDataRecord>> GetRecordsPOCAsync(
            object eventData,
            IDictionary<string, string> resolvedParams,
            IReadOnlyList<IDataRecord> dataset, 
            CancellationToken cancellationToken = default)
        {
            // For testing the resolved ternary/logical expression
            var resolvedEntityId = "<Not Resolved>";
            if (resolvedParams != null && resolvedParams.TryGetValue("EntityId", out var eid))
            {
                resolvedEntityId = eid;
            }

            var mockData = new
            {
                FetchedRelationId = Guid.NewGuid(),
                TargetEntityId = resolvedEntityId,
                Status = "Active"
            };

            var container = new { MemberRelation = new[] { mockData } };
            var records = TransformToIDataRecord.TransformFromObject(container, "");

            return await Task.FromResult(records);
        }
    }
}
