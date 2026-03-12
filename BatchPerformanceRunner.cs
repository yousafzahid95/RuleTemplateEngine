using System.Diagnostics;
using RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;

namespace RuleTemplateEngine.Benchmarks
{
    public class BatchPerformanceRunner
    {
        private readonly ITemplateParamResolver _templateResolver;
        private readonly IAntlrParamResolver _antlrResolver;

        public BatchPerformanceRunner(ITemplateParamResolver templateResolver, IAntlrParamResolver antlrResolver)
        {
            _templateResolver = templateResolver;
            _antlrResolver = antlrResolver;
        }

        public void RunAll()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("       BATCH PERFORMANCE COMPARISON              ");
            Console.WriteLine("=================================================");

            var iterations = new[] { 100, 1000, 10000 };
            
            // Setup shared mock data
            var mockEvent = new ExternalWorkplanTaskEvent
            {
                WorkplanTask = new WorkplanTaskData
                {
                    ProjectId = Guid.NewGuid(),
                    WorkAreaId = Guid.NewGuid(),
                    TaskId = Guid.NewGuid(),
                    EntityId = Guid.NewGuid(),
                    Entities = new List<WorkplanTaskEntity> { new() { WorkAreaEntityId = Guid.NewGuid() } }
                }
            };

            var dataset = new List<IDataRecord>();
            dataset.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));
            dataset.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

            var templateDesc = new TemplateParam
            {
                Template = "Review {0} for project {1}",
                Params = { "[Workplan.Entities[0].WorkAreaEntityId]", "[Event.ProjectId]" }
            };
            var antlrDesc = "Review {Workplan.Entities[0].WorkAreaEntityId} for project {Event.ProjectId}";

            foreach (var count in iterations)
            {
                Console.WriteLine($"\n[Running {count} Iterations]");
                
                // 1. TemplateParam
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    _templateResolver.Resolve(templateDesc, dataset);
                }
                sw.Stop();
                var templateTime = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine($"  TemplateParam Approach: {templateTime:F2} ms");

                // 2. ANTLR
                sw.Restart();
                for (int i = 0; i < count; i++)
                {
                    _antlrResolver.Resolve(antlrDesc, dataset);
                }
                sw.Stop();
                var antlrTime = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine($"  ANTLR Approach        : {antlrTime:F2} ms");

                var diff = templateTime / antlrTime;
                Console.WriteLine($"  Result: ANTLR is {diff:F1}x faster");
            }

            Console.WriteLine("\n=================================================");
        }
    }
}
