using RuleTemplateEngine.Events;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.Helpers;
using Newtonsoft.Json;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public class POCRunner
    {
        private readonly IAntlrParamResolver _antlrResolver;

        public POCRunner(IAntlrParamResolver antlrResolver)
        {
            _antlrResolver = antlrResolver;
        }

        public async Task Run()
        {
            await RunWorkplanScenario();
            await RunMemberRelationScenario();
        }

        private async Task RunWorkplanScenario()
        {
            Console.WriteLine("=== Starting ANTLR POC ===");

            var ruleJson = @"
            {
              ""_id"": ""51691d06-358b-4bb5-9f3b-841fcc4fddc8"",
              ""RuleName"": ""WPTASK"",
              ""Events"": [""ExternalWorkplanTaskEvent""],
              ""ActionItemTemplate"": {
                ""Description"":        ""{AllWorkplan[0].Name}"",
                ""TaskId"":             ""{AllWorkplan[0].RootTaskId}"",
                ""EntityId"":           ""{AllWorkplan[0].Entities[2].WorkAreaEntityId}"",
                ""SourceSystemKey"":    ""WPTASK_{AllWorkplan[0].Id}_{AllWorkplan[1].RootTaskId}"",
                ""ItemDefinitionGuid"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                ""SourceSystem"":       1
              },
              ""Filters"": {
                ""DataSources"": [
                  {
                    ""Key"": ""AllWorkplan"",
                    ""DataSourceParams"": {
                      ""WorkAreaId"": ""{EventMessage.WorkplanTask.WorkAreaId}"",
                      ""TaskId"":     ""{EventMessage.WorkplanTask.TaskId}""
                    }
                  }
                ]
              }
            }";
            var rule = JsonConvert.DeserializeObject<RuleModel>(ruleJson)
                ?? throw new InvalidOperationException("Failed to deserialize workplan rule.");

            // Mock Event
            var mockEvent = new ExternalWorkplanTaskEvent
            {
                WorkplanTask = new WorkplanTaskData
                {
                    TaskId = Guid.NewGuid(),
                    WorkAreaId = Guid.NewGuid()
                }
            };

            // Event context for resolving data source parameters
            var initialDatasets = new List<IDataRecord>();

            initialDatasets.AddRange(
                TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

            initialDatasets.AddRange(
                TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));

            // Resolve Data Source Params
            var adapter = new MockWorkplanAllTaskDataSource();
            var dsParams = rule.Filters.DataSources[0].DataSourceParams
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => _antlrResolver.Resolve(kvp.Value, initialDatasets)
                );

            Console.WriteLine($"Resolved DS Params: WorkAreaId={dsParams["WorkAreaId"]}, TaskId={dsParams["TaskId"]}");

            // Fetch Records
            var allWorkplanRecordsArray = await adapter.GetRecordsPOCAsync(mockEvent, dsParams, new List<IDataRecord>(), CancellationToken.None);
            var allWorkplanRecords = allWorkplanRecordsArray.ToList();

            Console.WriteLine($"Fetched {allWorkplanRecords.Count} records from AllWorkplan DataSource.");

            initialDatasets.AddRange(allWorkplanRecords);

            // Resolve Action Item
            var desc = _antlrResolver.Resolve(rule.ActionItemTemplate.Description, initialDatasets);
            var taskId = _antlrResolver.Resolve(rule.ActionItemTemplate.TaskId, initialDatasets); // AllWorkplan 
            var entityId = _antlrResolver.Resolve(rule.ActionItemTemplate.EntityId, initialDatasets);
            var srcSystemKey = _antlrResolver.Resolve(rule.ActionItemTemplate.SourceSystemKey, initialDatasets);

            Console.WriteLine($"\nResolved ActionItemTemplate Fields:");
            Console.WriteLine($"Description:      {desc}");
            Console.WriteLine($"TaskId:           {taskId}");
            Console.WriteLine($"EntityId:         {entityId}");
            Console.WriteLine($"SourceSystemKey:  {srcSystemKey}");

            Console.WriteLine("\n=== Testing All Supported Patterns ===");
            var testCases = new Dictionary<string, string>
            {
                { "Static text", "my static value" },
                { "Single path", "{AllWorkplan[0].RootTaskId}" },
                { "Event path", "{Event.TaskId}" },
                { "Mixed text + path", "WPTASK_{AllWorkplan[0].Id}_{AllWorkplan[0].RootTaskId}" },
                { "Array index", "{AllWorkplan[0].Entities[0].WorkAreaEntityId}" },
                { "Fallback", "{AllWorkplan.MissingProperty ?? Event.WorkAreaId}" },
                { "Literal brace", "value \\{not a ref\\}" }
            };

            foreach (var kvp in testCases)
            {
                var result = _antlrResolver.Resolve(kvp.Value, initialDatasets);
                Console.WriteLine($"{kvp.Key,-20} | {kvp.Value,-50} => {result}");
            }

            Console.WriteLine("=== End of POC ===");
        }

        private async Task RunMemberRelationScenario()
        {
            Console.WriteLine("\n=== Starting ExternalMemberRelationEventMessage Scenario ===");

            var ruleJson = @"
            {
              ""_id"": ""d4fb6d54-7fd2-4b20-a5d8-2a1eb72dc0c5"",
              ""RuleName"": ""MEMBER_RELATION"",
              ""Events"": [""ExternalMemberRelationEventMessage""],
              ""ActionItemTemplate"": {
                ""Description"":        ""Member relation for {MemberRelation.TargetEntityId} ({MemberRelation.RequestedEventType})"",
                ""TaskId"":             ""{MemberRelation.FetchedRelationId}"",
                ""EntityId"":           ""{MemberRelation.TargetEntityId}"",
                ""SourceSystemKey"":    ""MEMREL_{MemberRelation.TargetEntityId}_{MemberRelation.RequestedEventType}"",
                ""ItemDefinitionGuid"": ""3fa85f64-5717-4562-b3fc-2c963f66afa6"",
                ""SourceSystem"":       1
              },
              ""Filters"": {
                ""DataSources"": [
                  {
                    ""Key"": ""MemberRelation"",
                    ""DataSourceParams"": {
                      ""WorkareaId"": ""{Event.WorkareaId}"",
                      ""EntityId"":   ""{(EventMessage.EventType == \""RelationCreated\"" || EventMessage.EventType == \""RelationDeleted\"") ? (Event.ParentEntityId ?? Event.EntityId) : Event.EntityId}""
                    }
                  }
                ]
              }
            }";

            var rule = JsonConvert.DeserializeObject<RuleModel>(ruleJson)
                ?? throw new InvalidOperationException("Failed to deserialize member relation rule.");

            var scenarios = new[]
            {
                new
                {
                    Name = "RelationCreated uses ParentEntityId",
                    EventType = "RelationCreated",
                    WorkareaId = Guid.NewGuid(),
                    EntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    ParentEntityId = (Guid?)Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                },
                new
                {
                    Name = "RelationDeleted falls back to EntityId",
                    EventType = "RelationDeleted",
                    WorkareaId = Guid.NewGuid(),
                    EntityId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    ParentEntityId = (Guid?)null
                }
            };

            var adapter = new MockMemberRelationDataSource();

            foreach (var scenario in scenarios)
            {
                var memberRelationEvent = new MemberRelationExternalEvent
                {
                    WorkareaId = scenario.WorkareaId,
                    EntityId = scenario.EntityId,
                    ParentEntityId = scenario.ParentEntityId
                };

                var eventMessage = new ExternalMemberRelationEventMessage
                {
                    EventType = scenario.EventType,
                    Body = JsonConvert.SerializeObject(memberRelationEvent)
                };

                var initialDatasets = BuildMemberRelationDatasets(eventMessage);

                var dsParams = rule.Filters.DataSources[0].DataSourceParams
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => _antlrResolver.Resolve(kvp.Value, initialDatasets));

                var memberRelationRecordsArray = await adapter.GetRecordsPOCAsync(
                    eventMessage,
                    dsParams,
                    initialDatasets,
                    CancellationToken.None);

                var memberRelationRecords = memberRelationRecordsArray.ToList();
                initialDatasets.AddRange(memberRelationRecords);

                var description = _antlrResolver.Resolve(rule.ActionItemTemplate.Description, initialDatasets);
                var entityId = _antlrResolver.Resolve(rule.ActionItemTemplate.EntityId, initialDatasets);
                var sourceSystemKey = _antlrResolver.Resolve(rule.ActionItemTemplate.SourceSystemKey, initialDatasets);

                Console.WriteLine($"\nScenario:         {scenario.Name}");
                Console.WriteLine($"Resolved EntityId:{dsParams["EntityId"]}");
                Console.WriteLine($"Description:      {description}");
                Console.WriteLine($"EntityId:         {entityId}");
                Console.WriteLine($"SourceSystemKey:  {sourceSystemKey}");
            }

            Console.WriteLine("=== End of ExternalMemberRelationEventMessage Scenario ===");
        }

        private static List<IDataRecord> BuildMemberRelationDatasets(ExternalMemberRelationEventMessage eventMessage)
        {
            var typedEvent = JsonConvert.DeserializeObject<MemberRelationExternalEvent>(eventMessage.Body)
                ?? throw new InvalidOperationException("Failed to deserialize member relation event body.");

            var datasets = new List<IDataRecord>();
            datasets.AddRange(TransformToIDataRecord.TransformFromObject(typedEvent, "Event"));
            datasets.AddRange(TransformToIDataRecord.TransformFromObject(eventMessage, "EventMessage"));

            return datasets;
        }
    }
}
