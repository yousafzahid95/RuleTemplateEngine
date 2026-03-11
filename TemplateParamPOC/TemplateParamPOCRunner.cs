using RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

namespace RuleTemplateEngine.TemplateParamPOC
{
    public class TemplateParamPOCRunner
    {
        public static async Task Run()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("    TEMPLATE PARAM POC RUNNER (WPTASK)           ");
            Console.WriteLine("=================================================");

            string ruleJson = """
            {
              "_id": "51691d06-358b-4bb5-9f3b-841fcc4fddc8",
              "RuleName": "WPTASK",
              "Events": ["ExternalWorkplanTaskEvent"],
              "ActionItemTemplate": {
                "SourceSystem": 1,
                "ItemDefinitionGuid": "8f2b3c4d-5e6f-7081-92a3-b4c5d6e7f912",
                "Description": {
                  "params": ["[Workplan.Name]"],
                  "template": "{0}"
                },
                "TaskId": {
                  "params": ["[Workplan.RootTaskId]"],
                  "template": "{0}"
                },
                "EntityId": {
                  "params": ["[Workplan.Entities[0].WorkAreaEntityId]"],
                  "template": "{0}"
                },
                "SourceSystemKey": {
                  "params": ["[Workplan.Id]"],
                  "template": "WPTASK_{0}"
                }
              },
              "Filters": {
                "DataSources": [],
                "ValidationRules": []
              },
              "Checks": {
                "DataSources": [
                  {
                    "Key": "AllWorkplan",
                    "params": {
                      "WorkAreaId": {
                        "params": ["[EventMessage.WorkplanTask.WorkAreaId]"],
                        "template": "{0}"
                      },
                      "TaskId": {
                        "params": ["[EventMessage.WorkplanTask.TaskId]"],
                        "template": "{0}"
                      }
                    }
                  }
                ],
                "ValidationRules": []
              }
            }
            """;

            // Deserialize rule to use the structured TemplateParam configuration.
            // Note: using Newtonsoft here as that is what standard RuleTemplateEngine models use underneath.
            var rule = Newtonsoft.Json.JsonConvert.DeserializeObject<RuleTemplate>(ruleJson);

            // 1. Generate Mock Event
            var mockEvent = new ExternalWorkplanTaskEvent
            {
                WorkplanTask = new WorkplanTaskData
                {
                    ProjectId = Guid.NewGuid(),
                    WorkAreaId = Guid.NewGuid(),
                    TaskId = Guid.NewGuid(),
                    EntityId = Guid.NewGuid(),
                    Entities = new List<WorkplanTaskEntity>
                    {
                        new WorkplanTaskEntity { WorkAreaEntityId = Guid.NewGuid() }
                    }
                }
            };

            // Event context for resolving data source parameters
            var initialDatasets = new List<IDataRecord>();
            // Add root external event message
            initialDatasets.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));
            // Add nested body (as some older pipelines might expect "Event." instead of "EventMessage." for body props)
            initialDatasets.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

            IDataSourceAdapter adapter = new MockWorkplanAllTaskDataSource();
            // Use DI-compatible resolver instances instead of static methods
            var exprResolver = new TemplateEngine.ExpressionResolver();
            var templateResolver = new TemplateEngine.TemplateParamResolver(exprResolver);
            var finalDataset = new List<IDataRecord>(initialDatasets);

            Console.WriteLine("\n[1] Resolving Data Sources (Flat Params)...");

            foreach (var dsConfig in rule.Checks.DataSources)
            {
                Console.WriteLine($"    Processing DataSource: {dsConfig.Key}");

                // Evaluates the nested `TemplateParam` inside `params` on the dataset list
                var resolvedParams = new Dictionary<string, string>();
                foreach (var paramKvp in dsConfig.Params)
                {
                    string resolvedValue = templateResolver.Resolve(paramKvp.Value, finalDataset);
                    resolvedParams[paramKvp.Key] = resolvedValue;
                    Console.WriteLine($"      -> Resolved '{paramKvp.Key}': {resolvedValue}");
                }

                // Call Adapter (which internally simulates network request using parsed TaskId / WorkAreaId)
                var rawRecords = (await ((MockWorkplanAllTaskDataSource)adapter).GetRecordsPOCAsync(mockEvent, resolvedParams, finalDataset, CancellationToken.None)).ToList();
                
                // Hack to remap AllWorkplan to Workplan to match the provided JSON template
                if (rawRecords.Count > 0)
                {
                    var firstRec = rawRecords[0];

                    // Extract properties via the indexer (which uses string names)
                    var idStr = firstRec["AllWorkplan.Id"]?.ToString();
                    var waStr = firstRec["AllWorkplan.WorkAreaId"]?.ToString();
                    var rootStr = firstRec["AllWorkplan.RootTaskId"]?.ToString();
                    
                    var typedMock = new FullTaskDTO
                    {
                        Id = Guid.TryParse(idStr, out var id) ? id : Guid.NewGuid(),
                        WorkAreaId = Guid.TryParse(waStr, out var wa) ? wa : Guid.NewGuid(),
                        RootTaskId = Guid.TryParse(rootStr, out var root) ? root : Guid.NewGuid(),
                        Name = "Mocked Target Task",
                        Description = "This is a mocked target task from the simulated service.",
                        CreatedOn = DateTimeOffset.UtcNow,
                        CreatedBy = Guid.NewGuid(),
                        Entities = new[] 
                        {
                            new EntityInfoDTO { WorkAreaEntityId = Guid.Parse("11112222-3333-4444-5555-666677778888") }
                        }
                    };
                    var records = TransformToIDataRecord<FullTaskDTO>.TransformFromObject(typedMock, "Workplan");
                    finalDataset.AddRange(records);
                }
            }

            Console.WriteLine("\n[2] Resolving Action Item (Flat Params)...");

            var descParam = rule.ActionItemTemplate.Description;
            var descRes = descParam != null ? templateResolver.Resolve(descParam, finalDataset) : string.Empty;

            var entityIdParam = rule.ActionItemTemplate.EntityId;
            var entityIdRes = entityIdParam != null ? templateResolver.Resolve(entityIdParam, finalDataset) : string.Empty;

            var taskIdParam = rule.ActionItemTemplate.TaskId;
            var taskIdRes = taskIdParam != null ? templateResolver.Resolve(taskIdParam, finalDataset) : string.Empty;

            var sskParam = rule.ActionItemTemplate.SourceSystemKey;
            var sskRes = sskParam != null ? templateResolver.Resolve(sskParam, finalDataset) : string.Empty;


            Console.WriteLine("    Resolved Extracted Fields:");
            Console.WriteLine($"      Description     : {descRes}");
            Console.WriteLine($"      TaskId          : {taskIdRes}");
            Console.WriteLine($"      EntityId        : {entityIdRes}");
            Console.WriteLine($"      SourceSystemKey : {sskRes}");
            
            Console.WriteLine("\n=================================================");
            Console.WriteLine("                  END OF RUN                     ");
            Console.WriteLine("=================================================");
        }
    }
}
