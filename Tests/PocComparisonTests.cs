using NUnit.Framework;
using AntlrPoc = RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;

namespace RuleTemplateEngine.Tests;

[TestFixture]
public class PocComparisonTests
{
    // These would typically be provided via DI in the real application.
    private static readonly ITemplateParamResolver TemplateParamResolver =
        new TemplateEngine.TemplateParamResolver();

    private static readonly AntlrPoc.IAntlrParamResolver AntlrParamResolver =
        new AntlrPoc.AntlrParamResolver(
            new AntlrPoc.ExpressionResolver(new AntlrPoc.ExpressionCache()));

    private static (ExternalWorkplanTaskEvent evt,
        IReadOnlyList<IDataRecord> templateParamDataset,
        AntlrPoc.EvaluationContext antlrContext,
        IList<IDataRecord> allWorkplanRecords) BuildSharedContext()
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

        // TemplateParam world: EventMessage + Event + Workplan (fetched)
        var initial = new List<IDataRecord>();
        initial.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage"));
        initial.AddRange(TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event"));

        var allWorkplanDto = new FullTaskDTO
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

        var workplanRecords = TransformToIDataRecord<FullTaskDTO>.TransformFromObject(allWorkplanDto, "Workplan").ToList();
        initial.AddRange(workplanRecords);

        // ANTLR world: datasets keyed by name
        var antlrDatasets = new Dictionary<string, IList<IDataRecord>>
        {
            ["Event"] = TransformToIDataRecord.TransformFromObject(mockEvent.WorkplanTask, "Event").ToList(),
            ["EventMessage"] = TransformToIDataRecord.TransformFromObject(mockEvent, "EventMessage").ToList()
        };

        var allWorkplanRecords = TransformToIDataRecord<FullTaskDTO>.TransformFromObject(allWorkplanDto, "AllWorkplan").ToList();
        antlrDatasets["AllWorkplan"] = allWorkplanRecords;

        var antlrContext = new AntlrPoc.EvaluationContext(antlrDatasets);

        return (mockEvent, initial, antlrContext, allWorkplanRecords);
    }

    [Test]
    public void SimpleTemplate_BothPocs_ProduceSameLogicalValues()
    {
        var (_, dataset, antlrContext, _) = BuildSharedContext();

        // Simple: single placeholder, single expression
        var tpl = new TemplateParam
        {
            Template = "{0}",
            Params = { "[Workplan.RootTaskId]" }
        };

        var templateParamValue = TemplateParamResolver.Resolve(tpl, dataset);

        var antlrValue = AntlrParamResolver.Resolve("{AllWorkplan.RootTaskId}", antlrContext);

        Assert.That(templateParamValue, Is.Not.Empty);
        Assert.That(templateParamValue, Is.EqualTo(antlrValue));
    }

    [Test]
    public void MediumTemplate_BothPocs_ProduceSameLogicalValues()
    {
        var (_, dataset, antlrContext, _) = BuildSharedContext();

        var tpl = new TemplateParam
        {
            Template = "WPTASK_{0}_{1}",
            Params =
            {
                "[Workplan.Id]",
                "[Workplan.RootTaskId]"
            }
        };

        var templateParamValue = TemplateParamResolver.Resolve(tpl, dataset);

        var antlrValue = AntlrParamResolver.Resolve("WPTASK_{AllWorkplan.Id}_{AllWorkplan.RootTaskId}", antlrContext);

        Assert.That(templateParamValue, Is.Not.Empty);
        Assert.That(templateParamValue, Is.EqualTo(antlrValue));
    }

    [Test]
    public void ComplexTemplate_BothPocs_ProduceExpectedShape()
    {
        var (_, dataset, antlrContext, _) = BuildSharedContext();

        var tpl = new TemplateParam
        {
            Template = "Review {0} for project {1}",
            Params =
            {
                "[Workplan.Entities[0].WorkAreaEntityId]",
                "[Event.ProjectId]"
            }
        };

        var templateParamValue = TemplateParamResolver.Resolve(tpl, dataset);

        var antlrValue = AntlrParamResolver.Resolve("Review {AllWorkplan.Entities[0].WorkAreaEntityId} for project {Event.ProjectId}", antlrContext);

        Assert.That(templateParamValue, Does.StartWith("Review "));
        Assert.That(templateParamValue, Does.Contain(" for project "));
        Assert.That(templateParamValue, Is.EqualTo(antlrValue));
    }
}

