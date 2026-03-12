using RuleTemplateEngine.Events;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.Helpers;

namespace RuleTemplateEngine.ANTLRParamPOC
{
    public class POCRunner
    {
        public static async Task Run()
        {
            Console.WriteLine("=== Starting ANTLR POC ===");
            var cache = new ExpressionCache();
            var resolver = new ExpressionResolver(cache);

            var ruleJson = @"
{
  ""_id"": ""51691d06-358b-4bb5-9f3b-841fcc4fddc8"",
  ""RuleName"": ""WPTASK"",
  ""Events"": [""ExternalWorkplanTaskEvent""],
  ""ActionItemTemplate"": {
    ""Description"":        ""{AllWorkplan.Name}"",
    ""TaskId"":             ""{AllWorkplan.RootTaskId}"",
    ""EntityId"":           ""{AllWorkplan.Entities[0].WorkAreaEntityId}"",
    ""SourceSystemKey"":    ""WPTASK_{AllWorkplan.Id}_{AllWorkplan.RootTaskId}"",
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
            var rule = Newtonsoft.Json.JsonConvert.DeserializeObject<RuleModel>(ruleJson);

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

            var baseContext = new EvaluationContext(initialDatasets);

            // Resolve Data Source Params
            var adapter = new MockWorkplanAllTaskDataSource();
            var dsParams = rule.Filters.DataSources[0].DataSourceParams
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => resolver.Resolve(kvp.Value, baseContext)?.ToString() ?? ""
                );

            Console.WriteLine($"Resolved DS Params: WorkAreaId={dsParams["WorkAreaId"]}, TaskId={dsParams["TaskId"]}");

            // Fetch Records
            var allWorkplanRecordsArray = await adapter.GetRecordsPOCAsync(mockEvent, dsParams, new List<IDataRecord>(), CancellationToken.None);
            var allWorkplanRecords = allWorkplanRecordsArray.ToList();

            Console.WriteLine($"Fetched {allWorkplanRecords.Count} records from AllWorkplan DataSource.");

            initialDatasets.AddRange(allWorkplanRecords);

            var finalContext = new EvaluationContext(initialDatasets);

            // Resolve Action Item
            var desc = resolver.Resolve(rule.ActionItemTemplate.Description, finalContext);
            var taskId = resolver.Resolve(rule.ActionItemTemplate.TaskId, finalContext);
            var entityId = resolver.Resolve(rule.ActionItemTemplate.EntityId, finalContext);
            var srcSystemKey = resolver.Resolve(rule.ActionItemTemplate.SourceSystemKey, finalContext);

            Console.WriteLine($"\nResolved ActionItemTemplate Fields:");
            Console.WriteLine($"Description:      {desc}");
            Console.WriteLine($"TaskId:           {taskId}");
            Console.WriteLine($"EntityId:         {entityId}");
            Console.WriteLine($"SourceSystemKey:  {srcSystemKey}");

            Console.WriteLine("\n=== Testing All Supported Patterns ===");
            var testCases = new Dictionary<string, string>
            {
                { "Static text", "my static value" },
                { "Single path", "{AllWorkplan.RootTaskId}" },
                { "Event path", "{Event.TaskId}" },
                { "Mixed text + path", "WPTASK_{AllWorkplan.Id}_{AllWorkplan.RootTaskId}" },
                { "Array index", "{AllWorkplan.Entities[0].WorkAreaEntityId}" },
                { "Fallback", "{AllWorkplan.MissingProperty ?? Event.WorkAreaId}" },
                { "Literal brace", "value \\{not a ref\\}" }
            };

            foreach (var kvp in testCases)
            {
                var result = resolver.Resolve(kvp.Value, finalContext)?.ToString() ?? "null";
                Console.WriteLine($"{kvp.Key,-20} | {kvp.Value,-50} => {result}");
            }

            Console.WriteLine("=== End of POC ===");
        }
    }
}
