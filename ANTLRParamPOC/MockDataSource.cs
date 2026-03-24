using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public class MockWorkplanAllTaskDataSource : IDataSourceAdapter
    {
        public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
            object eventData,
            IDictionary<string, TemplateParam> dataSourceParams,
            IReadOnlyList<IDataRecord> dataset, 
            CancellationToken cancellationToken = default)
        {
            return await GetRecordsPOCAsync(eventData, null, dataset, cancellationToken);
        }

        public async Task<IEnumerable<IDataRecord>> GetRecordsPOCAsync(
            object eventData,
            IDictionary<string, string> resolvedParams,
            IReadOnlyList<IDataRecord> dataset, 
            CancellationToken cancellationToken = default)
        {
            if (eventData is null)
            {
                throw new InvalidOperationException("Event data cannot be null.");
            }

            if (eventData is not ExternalWorkplanTaskEvent && eventData is not SDTWorkplanReplayEvent)
            {
                throw new InvalidOperationException($"Event data must be of type {nameof(ExternalWorkplanTaskEvent)} or {nameof(SDTWorkplanReplayEvent)}.");
            }

            Guid workplanTaskId = Guid.Empty;
            Guid workAreaId = Guid.Empty;

            if (resolvedParams != null && resolvedParams.Count > 0)
            {
                if (resolvedParams.TryGetValue("WorkAreaId", out var waIdStr) && Guid.TryParse(waIdStr, out var wId))
                    workAreaId = wId;
                if (resolvedParams.TryGetValue("TaskId", out var tIdStr) && Guid.TryParse(tIdStr, out var tId))
                    workplanTaskId = tId;
            }
            else
            {
                if (eventData is ExternalWorkplanTaskEvent evt)
                {
                    workplanTaskId = evt.WorkplanTask.TaskId;
                    workAreaId = evt.WorkplanTask.WorkAreaId;
                }
                if (eventData is SDTWorkplanReplayEvent rep_evt)
                {
                    workplanTaskId = Guid.NewGuid(); // Mock fallback
                    workAreaId = rep_evt.WorkplanTask.WorkareaId;
                }
            }

            if (workAreaId == Guid.Empty)
            {
                throw new InvalidOperationException("Failed to resolve required GUID parameter 'WorkAreaId' from data source parameters.");
            }
            if (workplanTaskId == Guid.Empty)
            {
                throw new InvalidOperationException("Failed to resolve required GUID parameter 'TaskId' from data source parameters.");
            }

            // Mocked Data
            var responseData = new FullTaskDTO
            {
                Id = workplanTaskId,
                WorkAreaId = workAreaId,
                RootTaskId = Guid.NewGuid(),
                Name = "Mocked Target Task",
                Description = "This is a mocked target task from the simulated service.",
                CreatedOn = DateTimeOffset.UtcNow,
                CreatedBy = Guid.NewGuid(),
                Entities = new[] 
                {
                    new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("11112222-3333-4444-5555-666677778888") },
                    new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
                    new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("22222222-2222-2222-2222-222222222222") },
                },
            };

            var responseData1 = new FullTaskDTO
            {
                Id = Guid.NewGuid(),
                WorkAreaId = workAreaId,
                RootTaskId = Guid.NewGuid(),
                Name = "Mocked Target Task",
                Description = "This is a mocked target task from the simulated service.",
                CreatedOn = DateTimeOffset.UtcNow,
                CreatedBy = Guid.NewGuid(),
                Entities = new[]
                {
                    new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("11112222-3333-4444-5555-666677778888") }
                }
            };

            var responseData2 = new FullTaskDTO
            {
                Id = Guid.NewGuid(),
                WorkAreaId = workAreaId,
                RootTaskId = Guid.NewGuid(),
                Name = "Mocked Target Task",
                Description = "This is a mocked target task from the simulated service.",
                CreatedOn = DateTimeOffset.UtcNow,
                CreatedBy = Guid.NewGuid(),
                Entities = new[]
                {
                    new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("11112222-3333-4444-5555-666677778888") }
                }
            };
            var respObjs = new List<FullTaskDTO> { responseData, responseData1, responseData2 };
            var container = new { AllWorkplan = respObjs };
            var records = TransformToIDataRecord<FullTaskDTO>.TransformFromList(respObjs, "AllWorkplan");
            return await Task.FromResult(records);
        }
    }
}
