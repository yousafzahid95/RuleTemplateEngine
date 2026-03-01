using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;
using RuleTemplateEngine.Helpers;

namespace RuleTemplateEngine
{
    public class LemDataSourceAdapter : IDataSourceAdapter
    {
        /// <inheritdoc />
        /// <remarks>
        /// <paramref name="dataset"/> convention:
        /// - 1 record: dataset[0] = event body only → keyed as "Event" ([Event.ProjectId] etc.).
        /// - 2+ records: dataset[0] = EventMessage (message envelope), dataset[1] = EventData (Body) → keyed as "EventMessage" and "EventData"; "Event" is an alias for EventData for backward compatibility.
        /// LEM records are returned by the adapter; the caller appends them to the same list.
        /// Resolve uses only this list (keys are derived from position inside the engine).
        /// </remarks>
        public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
            object eventData,
            IDictionary<string, TemplateParam> dataSourceParams,
            IReadOnlyList<IDataRecord> dataset,
            CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"DataSource adapter {nameof(LemDataSourceAdapter)} execution starts.");

            if (eventData is null)
                throw new InvalidOperationException("Event data cannot be null.");

            if (dataSourceParams is null)
                throw new ArgumentNullException(nameof(dataSourceParams));

            var parameters = new ExtractedParameters
            {
                ProjectId = ResolveGuidParam("ProjectId", dataSourceParams, dataset),
                WorkAreaId = ResolveGuidParam("WorkAreaId", dataSourceParams, dataset),
                EntityId = ResolveGuidParam("EntityId", dataSourceParams, dataset),
                EffectiveDate = null,
                SearchOption = "Active",
                IncludeOutOfScope = false,
                IncludeCollections = true,
                IsProjectLevel = false
            };

            Console.WriteLine($"\n[Extraction Result] ProjectId={parameters.ProjectId}, WorkAreaId={parameters.WorkAreaId}, EntityId={parameters.EntityId}\n");

            return await GetWorkAreaEntityAsync(parameters).ConfigureAwait(false);
        }

        private static Guid ResolveGuidParam(
            string key,
            IDictionary<string, TemplateParam> dataSourceParams,
            IReadOnlyList<IDataRecord> dataset)
        {
            if (!dataSourceParams.TryGetValue(key, out var templateParam))
                return Guid.Empty;

            var resolved = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(templateParam, dataset);
            return Guid.TryParse(resolved, out var guid) ? guid : Guid.Empty;
        }

        #region API Calls

        private async Task<IEnumerable<IDataRecord>> GetWorkAreaEntityAsync(ExtractedParameters parameters)
        {
            try
            {
                Console.WriteLine($"\n[API Call] GetWorkAreaEntityAsync");
                Console.WriteLine($"  ProjectId: {parameters.ProjectId}");
                Console.WriteLine($"  WorkAreaId: {parameters.WorkAreaId}");
                Console.WriteLine($"  EntityId: {parameters.EntityId}");

                var response = await FetchWorkAreaEntityAsync(parameters).ConfigureAwait(false);
                return TransformToIDataRecord<EntityWorkAreaLevelDetailIntegrationDto>.TransformFromObject(response.Data, "LEM");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to fetch WorkArea entity data: {ex.Message}");
                throw new InvalidOperationException("Error while fetching WorkArea entity data: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Fetches work area entity from external API. Replace with real API client.
        /// </summary>
        private Task<WorkAreaEntityResponse> FetchWorkAreaEntityAsync(ExtractedParameters parameters)
        {
            var data = new EntityWorkAreaLevelDetailIntegrationDto
            {
                Id = parameters.EntityId,
                ProjectEntityId = parameters.EntityId,
                ProjectId = parameters.ProjectId,
                WorkAreaId = parameters.WorkAreaId,
                Name = "Test WorkArea Entity",
                TypeName = "Corporation",
                CategoryName = "Legal Entity",
                TaxClassificationTypeName = "Partnership",
                Status = parameters.SearchOption,
                LastModifiedDate = parameters.EffectiveDate ?? DateTime.UtcNow
            };
            return Task.FromResult(new WorkAreaEntityResponse { Data = data });
        }

        private Task<IEnumerable<IDataRecord>> GetProjectEntityAsync(ExtractedParameters parameters)
        {
            var data = new EntityWorkAreaLevelDetailIntegrationDto
            {
                Id = parameters.EntityId,
                ProjectEntityId = parameters.EntityId,
                ProjectId = parameters.ProjectId,
                WorkAreaId = parameters.WorkAreaId,
                Name = "Test Project Entity",
                Status = parameters.SearchOption
            };
            var response = new WorkAreaEntityResponse { Data = data };
            return Task.FromResult(TransformToIDataRecord<EntityWorkAreaLevelDetailIntegrationDto>.TransformFromObject(response.Data, "LEM"));
        }

        #endregion

        #region Helper Classes

        private class ExtractedParameters
        {
            public Guid ProjectId { get; set; }
            public Guid WorkAreaId { get; set; }
            public Guid EntityId { get; set; }
            public DateTime? EffectiveDate { get; set; }
            public string SearchOption { get; set; } = "Active";
            public bool IncludeOutOfScope { get; set; }
            public bool IncludeCollections { get; set; }
            public bool IsProjectLevel { get; set; }
        }

        private class WorkAreaEntityResponse
        {
            public EntityWorkAreaLevelDetailIntegrationDto Data { get; set; } = null!;
        }

        #endregion
    }
}
