using RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;

namespace RuleTemplateEngine.TemplateParamPOC
{
    public class TemplateParamPOCRunner
    {
        private readonly ITemplateParamResolver _templateResolver;

        public TemplateParamPOCRunner(ITemplateParamResolver templateResolver)
        {
            _templateResolver = templateResolver;
        }

        public async Task Run()
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

            var rule = Newtonsoft.Json.JsonConvert.DeserializeObject<RuleTemplate>(ruleJson);

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

            var initialDatasets = new List<IDataRecord>();
            initialDatasets.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));
            initialDatasets.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

            IDataSourceAdapter adapter = new MockWorkplanAllTaskDataSource();
            var finalDataset = new List<IDataRecord>(initialDatasets);

            Console.WriteLine("\n[1] Resolving Data Sources (Flat Params)...");

            foreach (var dsConfig in rule.Checks.DataSources)
            {
                Console.WriteLine($"    Processing DataSource: {dsConfig.Key}");

                var resolvedParams = new Dictionary<string, string>();
                foreach (var paramKvp in dsConfig.Params)
                {
                    string resolvedValue = _templateResolver.Resolve(paramKvp.Value, finalDataset);
                    resolvedParams[paramKvp.Key] = resolvedValue;
                    Console.WriteLine($"      -> Resolved '{paramKvp.Key}': {resolvedValue}");
                }

                var rawRecords = (await ((MockWorkplanAllTaskDataSource)adapter).GetRecordsPOCAsync(mockEvent, resolvedParams, finalDataset, CancellationToken.None)).ToList();
                
                if (rawRecords.Count > 0)
                {
                    var firstRec = rawRecords[0];
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
            var descRes = descParam != null ? _templateResolver.Resolve(descParam, finalDataset) : string.Empty;

            var entityIdParam = rule.ActionItemTemplate.EntityId;
            var entityIdRes = entityIdParam != null ? _templateResolver.Resolve(entityIdParam, finalDataset) : string.Empty;

            var taskIdParam = rule.ActionItemTemplate.TaskId;
            var taskIdRes = taskIdParam != null ? _templateResolver.Resolve(taskIdParam, finalDataset) : string.Empty;

            var sskParam = rule.ActionItemTemplate.SourceSystemKey;
            var sskRes = sskParam != null ? _templateResolver.Resolve(sskParam, finalDataset) : string.Empty;


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
