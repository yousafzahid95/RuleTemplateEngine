using System.Diagnostics;
using RuleTemplateEngine;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Events;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.Helpers;

Console.WriteLine("══════════════════════════════════════════════════════");
Console.WriteLine(" A002IR - LEM DataSource Parameter Resolution Tests");
Console.WriteLine("══════════════════════════════════════════════════════\n");

// Build A002IR-style LEM DataSource params (Filters.DataSources[\"LEM\"].params)
var lemParams = BuildLemParamsForA002Ir();
var actionItemTemplate = BuildA002IrActionItemTemplate();
const string RuleName = "A002IR";

await RunInfoRequestTestAsync(lemParams, actionItemTemplate, RuleName);
await RunLemReplayTestAsync(lemParams, actionItemTemplate, RuleName);
await RunExternalWorkplanTaskTestAsync(lemParams, actionItemTemplate, RuleName);
await RunMultipleLemRecordsTestAsync(actionItemTemplate, RuleName);

Console.WriteLine("\nAll tests completed.");

// ══════════════════════════════════════════════════════
//  Benchmark: V1 (flat List<string>) vs V2 (2D List<List<string>>)
// ══════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine(" Benchmark: V1 (flat) vs V2 (2D) TemplateParam");
Console.WriteLine("══════════════════════════════════════════════════════\n");

RunBenchmark();

// ----------------- Helpers -----------------

static Dictionary<string, TemplateParam> BuildLemParamsForA002Ir()
{
    return new Dictionary<string, TemplateParam>(StringComparer.OrdinalIgnoreCase)
    {
        ["ProjectId"] = new TemplateParam
        {
            Template = "{0}",
            Params = { "[Event.ProjectId]" }
        },
        ["WorkAreaId"] = new TemplateParam
        {
            Template = "{0}",
            Params = { "[Event.WorkareaId]", "[Event.WorkAreaId]" }
        },
        ["EntityId"] = new TemplateParam
        {
            Template = "{0}",
            Params = { "[Event.EntityIds[0]]", "[Event.Entities[0].WorkAreaEntityId]", "[Event.EntityId]" }
        }
    };
}

static ActionItemTemplateDefinition BuildA002IrActionItemTemplate()
{
    // Mirrors your A002IR ActionItemTemplate example, extended with dynamic
    // EntityId / TaskId / SourceSystemKey resolution via the template engine.
    return new ActionItemTemplateDefinition
    {
        SourceSystem = 1,
        ItemDefinitionId = 3,
        Description = new TemplateParam
        {
            Template = "Review {0} for project {1}",
            Params = { "[LEM.Name]", "[Event.ProjectId]" }
        },
        EntityId = new TemplateParam
        {
            Template = "{0}",
            Params = { "[LEM.EntityId]" }
        },
        TaskId = new TemplateParam
        {
            Template = "{0}",
            Params = { "[LEM.EntityId]" }
        },
        SourceSystemKey = new TemplateParam
        {
            Template = "A002IR_{0}_{1}",
            Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
        }
    };
}

static async Task RunInfoRequestTestAsync(
    IDictionary<string, TemplateParam> lemParams,
    ActionItemTemplateDefinition actionTemplate,
    string ruleName)
{
    Console.WriteLine("Test 1: InfoRequestEvent with EventMessage + EventData (message envelope + body)\n");

    var evt = new InfoRequestEvent
    {
        InfoRequest = new InfoRequestData
        {
            ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            WorkareaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            EntityIds = new List<Guid> { Guid.Parse("33333333-3333-3333-3333-333333333333") }
        }
    };

    var message = new ExternalGenericEntityEventMessage
    {
        MessageId = Guid.NewGuid(),
        Label = "InfoRequest",
        Timestamp = DateTime.UtcNow,
        EventType = "InfoRequest",
        Body = evt.InfoRequest
    };

    // Dataset: EventMessage = message envelope, Event = event body (keys match rule expressions [EventMessage.xxx], [Event.xxx])
    var datasetList = new List<IDataRecord>
    {
        TransformToIDataRecord<ExternalGenericEntityEventMessage>.TransformFromObject(message, "EventMessage").First(),
        TransformToIDataRecord<InfoRequestData>.TransformFromObject(evt.InfoRequest, "Event").First()
    };

    IDataSourceAdapter adapter = new LemDataSourceAdapter();
    var records = (await adapter.GetRecordsAsync(evt, lemParams, datasetList, CancellationToken.None)).ToList();
    datasetList.AddRange(records);

    var lemRecord = records.First();

    var actionItem = BuildActionItem(ruleName, actionTemplate, datasetList, lemRecord);

    Console.WriteLine("Resolved LEM dummy record:");
    foreach (var col in lemRecord.Columns)
        Console.WriteLine($"  {col} = {lemRecord[col]}");

    Console.WriteLine("\nResolved ActionItem:");
    Console.WriteLine($"  EntityId        = {actionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId      = {actionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId          = {actionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey = {actionItem.SourceSystemKey}");
    Console.WriteLine($"  Description     = {actionItem.Description}");

    Console.WriteLine();
}

static async Task RunLemReplayTestAsync(
    IDictionary<string, TemplateParam> lemParams,
    ActionItemTemplateDefinition actionTemplate,
    string ruleName)
{
    Console.WriteLine("Test 2: SDTLemReplayEvent only\n");

    var evt = new SDTLemReplayEvent
    {
        LemEvent = new LemEventData
        {
            ProjectId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            WorkareaId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            EntityId = Guid.Parse("66666666-6666-6666-6666-666666666666")
        }
    };

    // Single list: event record first (prefix "Event")
    var datasetList = new List<IDataRecord>
    {
        TransformToIDataRecord<LemEventData>.TransformFromObject(evt.LemEvent, "Event").First()
    };

    IDataSourceAdapter adapter = new LemDataSourceAdapter();
    var records = (await adapter.GetRecordsAsync(evt, lemParams, datasetList, CancellationToken.None)).ToList();
    datasetList.AddRange(records);

    var lemRecord = records.First();

    var actionItem = BuildActionItem(ruleName, actionTemplate, datasetList, lemRecord);

    Console.WriteLine("Resolved LEM dummy record:");
    foreach (var col in lemRecord.Columns)
        Console.WriteLine($"  {col} = {lemRecord[col]}");

    Console.WriteLine("\nResolved ActionItem:");
    Console.WriteLine($"  EntityId        = {actionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId      = {actionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId          = {actionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey = {actionItem.SourceSystemKey}");
    Console.WriteLine($"  Description     = {actionItem.Description}");

    Console.WriteLine();
}

static async Task RunExternalWorkplanTaskTestAsync(
    IDictionary<string, TemplateParam> lemParams,
    ActionItemTemplateDefinition actionTemplate,
    string ruleName)
{
    Console.WriteLine("Test 3: ExternalWorkplanTaskEvent only\n");

    var evt = new ExternalWorkplanTaskEvent
    {
        WorkplanTask = new WorkplanTaskData
        {
            ProjectId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            WorkAreaId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            TaskId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            EntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Entities = new List<WorkplanTaskEntity>
            {
                new WorkplanTaskEntity
                {
                    WorkAreaEntityId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                }
            }
        }
    };

    // Single list: event record first (prefix "Event")
    var datasetList = new List<IDataRecord>
    {
        TransformToIDataRecord<WorkplanTaskData>.TransformFromObject(evt.WorkplanTask, "Event").First()
    };

    IDataSourceAdapter adapter = new LemDataSourceAdapter();
    var records = (await adapter.GetRecordsAsync(evt, lemParams, datasetList, CancellationToken.None)).ToList();
    datasetList.AddRange(records);

    var lemRecord = records.First();

    var actionItem = BuildActionItem(ruleName, actionTemplate, datasetList, lemRecord);

    Console.WriteLine("Resolved LEM dummy record:");
    foreach (var col in lemRecord.Columns)
        Console.WriteLine($"  {col} = {lemRecord[col]}");

    Console.WriteLine("\nResolved ActionItem:");
    Console.WriteLine($"  EntityId        = {actionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId      = {actionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId          = {actionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey = {actionItem.SourceSystemKey}");
    Console.WriteLine($"  Description     = {actionItem.Description}");

    Console.WriteLine();
}

static Task RunMultipleLemRecordsTestAsync(
    ActionItemTemplateDefinition actionTemplate,
    string ruleName)
{
    Console.WriteLine("Test 4: Multiple LEM records - [LEM.EntityId] vs [LEM[2].EntityId]\n");

    var lemDtos = new List<EntityWorkAreaLevelDetailIntegrationDto>
    {
        new() { Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333") },
        new() { Id = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333") },
        new() { Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("cccccccc-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("cccccccc-3333-3333-3333-333333333333") }
    };
    var lemRecords = TransformToIDataRecord<EntityWorkAreaLevelDetailIntegrationDto>.TransformFromList(lemDtos, "LEM").ToList();

    // Keys come from record column names (e.g. "LEM.EntityId" -> LEM). So just pass the 3 LEM records.
    var datasetList = new List<IDataRecord>(lemRecords);

    // Resolve via RuleTemplateEngine only (single public API)
    var firstEntityId = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(
        new TemplateParam { Template = "{0}", Params = { "[LEM.EntityId]" } },
        datasetList);
    var firstByIndex = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(
        new TemplateParam { Template = "{0}", Params = { "[LEM[0].EntityId]" } },
        datasetList);
    Console.WriteLine($"  [LEM.EntityId]     = {firstEntityId}");
    Console.WriteLine($"  [LEM[0].EntityId]  = {firstByIndex}");

    var thirdEntityId = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(
        new TemplateParam { Template = "{0}", Params = { "[LEM[2].EntityId]" } },
        datasetList);
    Console.WriteLine($"  [LEM[2].EntityId]  = {thirdEntityId}");

    // Action item using first LEM (default template)
    var actionFromFirst = BuildActionItem(ruleName, actionTemplate, datasetList, lemRecords[0]);
    Console.WriteLine("\n  ActionItem from [LEM] / [LEM[0]]:");
    Console.WriteLine($"    SourceSystemKey = {actionFromFirst.SourceSystemKey}");
    Console.WriteLine($"    EntityId        = {actionFromFirst.EntityId}");

    // Template that uses [LEM[2].EntityId] for SourceSystemKey
    var templateUseThird = new ActionItemTemplateDefinition
    {
        SourceSystem = actionTemplate.SourceSystem,
        ItemDefinitionId = actionTemplate.ItemDefinitionId,
        Description = actionTemplate.Description,
        EntityId = new TemplateParam
        {
            Template = "{0}",
            Params = { "[LEM[2].EntityId]" }
        },
        TaskId = new TemplateParam
        {
            Template = "{0}",
            Params = { "[LEM[2].EntityId]" }
        },
        SourceSystemKey = new TemplateParam
        {
            Template = "A002IR_{0}",
            Params = { "[LEM[2].EntityId]" }
        }
    };
    var actionFromThird = BuildActionItem(ruleName, templateUseThird, datasetList, lemRecords[2]);
    Console.WriteLine("\n  ActionItem from [LEM[2]]:");
    Console.WriteLine($"    SourceSystemKey = {actionFromThird.SourceSystemKey}");
    Console.WriteLine($"    EntityId        = {actionFromThird.EntityId}");

    Console.WriteLine();
    return Task.CompletedTask;
}

static void RunBenchmark()
{
    // Build a dataset with 3 LEM records
    var lemDtos = new List<EntityWorkAreaLevelDetailIntegrationDto>
    {
        new() { Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333") },
        new() { Id = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333") },
        new() { Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("cccccccc-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("cccccccc-3333-3333-3333-333333333333") }
    };
    var lemRecords = TransformToIDataRecord<EntityWorkAreaLevelDetailIntegrationDto>.TransformFromList(lemDtos, "LEM").ToList();
    var datasetList = new List<IDataRecord>(lemRecords);

    // ── V1 (flat) ──
    // 2-placeholder, direct hit (no fallback needed)
    var v1Direct = new TemplateParam
    {
        Template = "A002IR_{0}_{1}",
        Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
    };

    // ── V2 with 1 expression per placeholder (best case) ──
    var v2Single = new TemplateParamV2
    {
        Template = "A002IR_{0}_{1}",
        Params = new List<List<string>>
        {
            new() { "[LEM.EntityId]" },
            new() { "[LEM.WorkAreaId]" }
        }
    };

    // ── V2 with 5 expressions per placeholder (4 nulls then hit) ──
    var v2Deep = new TemplateParamV2
    {
        Template = "A002IR_{0}_{1}",
        Params = new List<List<string>>
        {
            new() { "[LEM.Missing1]", "[LEM.Missing2]", "[LEM.Missing3]", "[LEM.Missing4]", "[LEM.EntityId]" },
            new() { "[LEM.Nope1]", "[LEM.Nope2]", "[LEM.Nope3]", "[LEM.Nope4]", "[LEM.WorkAreaId]" }
        }
    };

    // Correctness check
    var v1Result = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(v1Direct, datasetList);
    var v2SingleResult = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Single, datasetList);
    var v2DeepResult = RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Deep, datasetList);

    Console.WriteLine("Correctness check:");
    Console.WriteLine($"  V1 direct       = {v1Result}");
    Console.WriteLine($"  V2 single (1)   = {v2SingleResult}");
    Console.WriteLine($"  V2 deep   (5)   = {v2DeepResult}");
    Console.WriteLine($"  All match       = {v1Result == v2SingleResult && v1Result == v2DeepResult}");

    // Benchmark
    const int warmup = 1_000;
    const int iterations = 100_000;

    // Warmup all paths
    for (var i = 0; i < warmup; i++)
    {
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(v1Direct, datasetList);
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Single, datasetList);
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Deep, datasetList);
    }

    // V1 flat
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++)
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(v1Direct, datasetList);
    sw.Stop();
    var v1Ms = sw.Elapsed.TotalMilliseconds;

    // V2 single (1 per placeholder)
    sw.Restart();
    for (var i = 0; i < iterations; i++)
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Single, datasetList);
    sw.Stop();
    var v2SingleMs = sw.Elapsed.TotalMilliseconds;

    // V2 deep (5 per placeholder, 4 nulls + 1 hit)
    sw.Restart();
    for (var i = 0; i < iterations; i++)
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.ResolveV2(v2Deep, datasetList);
    sw.Stop();
    var v2DeepMs = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"\nBenchmark ({iterations:N0} iterations):");
    Console.WriteLine($"  {"Scenario",-35} {"Time",-15} {"vs V1",-12}");
    Console.WriteLine($"  {"--------",-35} {"----",-15} {"-----",-12}");
    Console.WriteLine($"  {"V1  flat (2 params)",-35} {v1Ms,10:F2} ms {"baseline",12}");
    Console.WriteLine($"  {"V2  1 expr/placeholder (2x1)",-35} {v2SingleMs,10:F2} ms {(v2SingleMs - v1Ms),+9:F2} ms");
    Console.WriteLine($"  {"V2  5 expr/placeholder (2x5)",-35} {v2DeepMs,10:F2} ms {(v2DeepMs - v1Ms),+9:F2} ms");
    Console.WriteLine();
}

static ActionItem BuildActionItem(
    string ruleName,
    ActionItemTemplateDefinition template,
    IReadOnlyList<IDataRecord> dataset,
    IDataRecord lemRecord)
{
    var description = template.Description != null
        ? RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(template.Description, dataset)
        : string.Empty;

    var entityIdStr = template.EntityId != null
        ? RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(template.EntityId, dataset)
        : string.Empty;

    var taskIdStr = template.TaskId != null
        ? RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(template.TaskId, dataset)
        : string.Empty;

    var sourceKey = template.SourceSystemKey != null
        ? RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(template.SourceSystemKey, dataset)
        : string.Empty;

    var workAreaIdVal = lemRecord["WorkAreaId"];
    var actionItem = new ActionItem
    {
        EntityId = Guid.TryParse(entityIdStr, out var eid) ? eid : Guid.Empty,
        WorkAreaId = workAreaIdVal != null && Guid.TryParse(workAreaIdVal.ToString(), out var waid) ? waid : Guid.Empty,
        TaskId = Guid.TryParse(taskIdStr, out var tid) ? tid : Guid.Empty,
        SourceSystemKey = sourceKey,
        Description = description,
        Status = "Open"
    };

    return actionItem;
}
