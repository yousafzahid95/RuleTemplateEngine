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
            if (eventData is not ExternalMemberRelationEventMessage eventMessage)
            {
                throw new InvalidOperationException(
                    $"Event data must be of type {nameof(ExternalMemberRelationEventMessage)}.");
            }

            if (!string.Equals(eventMessage.EventType, "RelationCreated", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(eventMessage.EventType, "RelationDeleted", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Unsupported member relation event type '{eventMessage.EventType}'.");
            }

            if (resolvedParams is null || !resolvedParams.TryGetValue("EntityId", out var resolvedEntityId) ||
                string.IsNullOrWhiteSpace(resolvedEntityId))
            {
                throw new InvalidOperationException(
                    "Failed to resolve required parameter 'EntityId' for member relation lookup.");
            }

            resolvedParams.TryGetValue("WorkareaId", out var resolvedWorkareaId);

            var mockData = new
            {
                FetchedRelationId = Guid.NewGuid(),
                RequestedEventType = eventMessage.EventType,
                TargetEntityId = resolvedEntityId,
                WorkareaId = resolvedWorkareaId ?? string.Empty,
                Status = "Active"
            };

            var records = TransformToIDataRecord.TransformFromObject(mockData, "MemberRelation");

            return await Task.FromResult(records);
        }
    }
}
