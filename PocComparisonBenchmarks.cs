using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using AntlrPoc = RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;

[MemoryDiagnoser]
public class PocComparisonBenchmarks
{
    private IReadOnlyList<IDataRecord> _templateParamDataset = null!;
    private AntlrPoc.EvaluationContext _antlrContext = null!;
    private TemplateParam _templateParamDesc = null!;
    private TemplateParam _templateParamSsk = null!;

    // These would typically be injected via DI in a real host.
    private ITemplateParamResolver _templateParamResolver = null!;
    private AntlrPoc.IAntlrParamResolver _antlrParamResolver = null!;

    [GlobalSetup]
    public void Setup()
    {
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
                    new() { WorkAreaEntityId = Guid.NewGuid() }
                }
            }
        };

        var initial = new List<IDataRecord>();
        initial.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));
        initial.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

        var dto = new FullTaskDTO
        {
            Id = mockEvent.WorkplanTask.TaskId,
            WorkAreaId = mockEvent.WorkplanTask.WorkAreaId,
            RootTaskId = Guid.NewGuid(),
            Name = "Mocked Target Task",
            Description = "This is a mocked target task from the simulated service.",
            CreatedOn = DateTimeOffset.UtcNow,
            CreatedBy = Guid.NewGuid(),
            Entities = new[]
            {
                new EntityInfoDTO { WorkAreaEntityId = mockEvent.WorkplanTask.Entities[0].WorkAreaEntityId }
            }
        };

        var workplanRecords = TransformToIDataRecord<FullTaskDTO>.TransformFromObject(dto, "Workplan").ToList();
        initial.AddRange(workplanRecords);
        _templateParamDataset = initial;

        var datasets = TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event")
        .Concat(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"))
        .Concat(TransformToIDataRecord<FullTaskDTO>.TransformFromObject(dto, "AllWorkplan"))
        .ToList();

        _antlrContext = new AntlrPoc.EvaluationContext(datasets);

        _templateParamResolver = new TemplateParamResolver(new ExpressionResolver());

        _antlrParamResolver = new AntlrPoc.AntlrParamResolver(
            new AntlrPoc.ExpressionResolver(new AntlrPoc.ExpressionCache()));

        _templateParamDesc = new TemplateParam
        {
            Template = "Review {0} for project {1}",
            Params =
            {
                "[Workplan.Entities[0].WorkAreaEntityId]",
                "[Event.ProjectId]"
            }
        };

        _templateParamSsk = new TemplateParam
        {
            Template = "WPTASK_{0}_{1}",
            Params =
            {
                "[Workplan.Id]",
                "[Workplan.RootTaskId]"
            }
        };
    }

    [Benchmark(Description = "TemplateParam POC - Description")]
    public string TemplateParam_Description() =>
        _templateParamResolver.Resolve(_templateParamDesc, _templateParamDataset);

    [Benchmark(Description = "ANTLR POC - Description")]
    public string Antlr_Description() =>
        _antlrParamResolver.Resolve("Review {AllWorkplan.Entities[0].WorkAreaEntityId} for project {Event.ProjectId}", _antlrContext);

    [Benchmark(Description = "TemplateParam POC - SourceSystemKey")]
    public string TemplateParam_SourceSystemKey() =>
        _templateParamResolver.Resolve(_templateParamSsk, _templateParamDataset);

    [Benchmark(Description = "ANTLR POC - SourceSystemKey")]
    public string Antlr_SourceSystemKey() =>
        _antlrParamResolver.Resolve("WPTASK_{AllWorkplan.Id}_{AllWorkplan.RootTaskId}", _antlrContext);
}

