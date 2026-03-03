# ADR: Rule template params — flat list vs per-placeholder fallback lists

## Status

TBD.

## Context

Rule templates resolve placeholders (`{0}`, `{1}`, …) from a dataset using expressions like `[Workplan.Id]` or `[Event.ProjectId]`. Each placeholder can be bound to one expression or to multiple fallback expressions (first non-empty wins). This ADR compares two ways to represent that in rule configuration and in the engine.

---

## Option 1: Flat params (single list — `params[i]` → `{i}`)

**Idea:** `params` is a single array of strings. Slot `i` in the array maps to placeholder `{i}`. Fallbacks are only supported when the template has a single placeholder `"{0}"`: then multiple entries in `params` are tried in order until one resolves non-empty.

### JSON rule config (Option 1)

Full rule using flat params. EntityId uses fallbacks (template `"{0}"`); Description and SourceSystemKey use one expression per placeholder.

```json
{
  "_id": "51691d06-358b-4bb5-9f3b-841fcc4fddc8",
  "RuleName": "WPTASK",
  "Events": ["ExternalWorkplanTaskEvent"],
  "ActionItemTemplateDefinition": {
    "SourceSystem": 1,
    "ItemDefinitionGuid": "8f2b3c4d-5e6f-7081-92a3-b4c5d6e7f912",
    "Description": {
      "Params": ["[Workplan.Name]"],
      "Template": "{0}"
    },
    "TaskId": {
      "Params": ["[Workplan.RootTaskId]"],
      "Template": "{0}"
    },
    "EntityId": {
      "Params": [
        "[Event.EntityIds[0]]",
        "[Event.Entities[0].WorkAreaEntityId]",
        "[Event.EntityId]"
      ],
      "Template": "{0}"
    },
    "SourceSystemKey": {
      "Params": ["[Workplan.Id]"],
      "Template": "WPTASK_{0}"
    }
  },
  "Filters": {
    "DataSources": [
      {
        "Key": "AllWorkplan",
        "DataSourceParams": {
          "WorkAreaId": {
            "Params": ["[EventMessage.WorkplanTask.WorkAreaId]"],
            "Template": "{0}"
          },
          "TaskId": {
            "Params": ["[EventMessage.WorkplanTask.TaskId]"],
            "Template": "{0}"
          }
        }
      }
    ],
    "ValidationRules": []
  },
  "Checks": {
    "DataSources": [],
    "ValidationRules": []
  }
}
```

- **EntityId:** Template is exactly `"{0}"`, so the three entries in `Params` are used as fallbacks (first non-empty wins). This is the only case in flat params where fallbacks work.
- **Description / SourceSystemKey:** One expression per placeholder. No fallbacks for `{0}` and `{1}` in the same template.

**Risk with flat params when using multiple placeholders:** If we want `"Review {0} for project {1}"` with fallbacks for both (e.g. {0} ← LEM.Name or LEM.CategoryName, {1} ← Event.ProjectId), we cannot express it in one rule. Putting four strings in `Params` would map `params[0]`→{0}, `params[1]`→{1}, `params[2]`→{2}, `params[3]`→{3}; the template only has {0} and {1}, so the engine would use the first two values and we’d get “Review (first) for project (second)” — but the “second” value would actually be the second candidate for {0}, not for {1}. So the flow breaks. With flat params, for any template with multiple placeholders you must have **exactly as many values as placeholders** (e.g. 3 placeholders → 3 entries in `Params`), and you need to **know for sure** that each of those expressions will resolve to a value; there is no per-placeholder fallback. If any slot can be missing, flat params cannot express fallbacks for that slot without breaking the mapping for the others.

---

## Option 2: Per-placeholder fallback lists (2D params)

**Idea:** `params` is an array of arrays. `params[i]` is the list of candidate expressions for placeholder `{i}`. For each placeholder, the engine tries the candidates in order and uses the first non-empty value. Fallbacks work for every placeholder and any template.

### JSON rule config (Option 2)

Same rule shape with 2D params. EntityId and Description can both use per-placeholder fallbacks.

```json
{
  "_id": "51691d06-358b-4bb5-9f3b-841fcc4fddc9",
  "RuleName": "WPTASK_2D",
  "Events": ["ExternalWorkplanTaskEvent"],
  "ActionItemTemplateDefinition": {
    "SourceSystem": 1,
    "ItemDefinitionGuid": "8f2b3c4d-5e6f-7081-92a3-b4c5d6e7f912",
    "Description": {
      "Params": [
        ["[LEM.MissingName]", "[LEM.Name]", "[LEM.CategoryName]"],
        ["[Event.ProjectName]", "[Event.ProjectId]"]
      ],
      "Template": "Review {0} for project {1}"
    },
    "TaskId": {
      "Params": [["[Workplan.RootTaskId]"]],
      "Template": "{0}"
    },
    "EntityId": {
      "Params": [
        [
          "[Event.EntityIds[0]]",
          "[Event.Entities[0].WorkAreaEntityId]",
          "[Event.EntityId]"
        ]
      ],
      "Template": "{0}"
    },
    "SourceSystemKey": {
      "Params": [
        ["[Workplan.Id]", "[EventData.WorkplanTask.Id]"],
        ["[EventData.WorkplanTask.WorkAreaId]", "[EventData.WorkplanTask.WorkareaId]"]
      ],
      "Template": "WPTASK_{0}_{1}"
    }
  },
  "Filters": {
    "DataSources": [
      {
        "Key": "AllWorkplan",
        "DataSourceParams": {
          "WorkAreaId": {
            "Params": [["[EventMessage.WorkplanTask.WorkAreaId]"]],
            "Template": "{0}"
          },
          "TaskId": {
            "Params": [["[EventMessage.WorkplanTask.TaskId]"]],
            "Template": "{0}"
          }
        },
        "Params": {}
      }
    ],
    "ValidationRules": []
  },
  "Checks": {
    "DataSources": [],
    "ValidationRules": []
  }
}
```

- **EntityId:** One inner list with three candidates for `{0}`; first non-empty wins.
- **Description:** Two inner lists: first list = candidates for `{0}`, second = candidates for `{1}`. No cross-talk; each placeholder has its own fallback chain.
- **SourceSystemKey:** Two placeholders, each with two candidates. Safe and unambiguous.

Full examples are in `Docs/WPTASK-mongo-example.json` (both documents).

---

## C# mapping (engine)

Below is how the two options map to the current engine types. The same semantics are reflected in the JSON above.

### Option 1 — Flat (TemplateParam)

One expression per placeholder for multi-placeholder templates:

```csharp
SourceSystemKey = new TemplateParam
{
    Template = "A002IR_{0}_{1}",
    Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
};
```

Fallback only works with a single `"{0}"` placeholder — all params become fallback candidates for that one slot:

```csharp
EntityId = new TemplateParam
{
    Template = "{0}",
    Params = { "[Event.EntityIds[0]]", "[Event.Entities[0].WorkAreaEntityId]", "[Event.EntityId]" }
};
```

### Option 2 — Per-placeholder fallback lists (TemplateParamV2)

Each placeholder gets its own independent fallback chain:

```csharp
SourceSystemKey = new TemplateParamV2
{
    Template = "A002IR_{0}_{1}",
    Params = new List<List<string>>
    {
        new() { "[LEM.EntityId]" },
        new() { "[LEM.WorkAreaId]" }
    }
};

Description = new TemplateParamV2
{
    Template = "Review {0} for project {1}",
    Params = new List<List<string>>
    {
        new() { "[LEM.MissingName]", "[LEM.Name]", "[LEM.EntityId]" },  // {0} fallbacks
        new() { "[Event.ProjectName]", "[Event.ProjectId]" }              // {1} fallbacks
    }
};
```

---

## Comparison

| Aspect | Option 1: Flat params | Option 2: Per-placeholder fallback lists |
|--------|------------------------|------------------------------------------|
| **Shape** | `params`: array of strings | `params`: array of arrays of strings |
| **Mapping** | `params[i]` → `{i}` | `params[i]` = candidate list for `{i}` |
| **Fallback for single `{0}`** | Yes (multiple entries = try in order) | Yes (one inner list with N candidates) |
| **Fallback for `{0}` and `{1}` in one template** | No; would break mapping | Yes; each placeholder has its own list |
| **JSON size** | Smaller (no extra brackets) | Slightly larger (nested arrays) |
| **Mental model** | “One value per slot” (or “all for {0}” when template is `"{0}"`) | “Per-slot list of alternatives” |
| **Risk** | Easy to mis-use in multi-placeholder templates (extra values map to non-existent placeholders) | Structure matches semantics; no ambiguity |

---

## Benchmarks

Benchmarks compare resolution cost and memory for the same logical outcome (e.g. `A002IR_{0}_{1}` with two expressions) in both shapes, and for 2D with multiple fallbacks per placeholder.

### How we benchmark

- **Dataset:** 50 LEM records (same for all runs), built once per benchmark run.
- **Scenarios:**
  - **Flat, 2 params:** `TemplateParam` with `Params = ["[LEM.EntityId]", "[LEM.WorkAreaId]"]` (one expression per placeholder).
  - **2D, 1 expr/placeholder:** `TemplateParamV2` with `Params = [["[LEM.EntityId]"], ["[LEM.WorkAreaId]"]]` (same expressions, 2D shape).
  - **2D, 5 exprs/placeholder:** `TemplateParamV2` with two inner lists of 5 candidates each (4 missing paths + 1 hit) to stress fallback iteration.

Correctness is checked first: all three produce the same string for the same dataset. Then we measure:

1. **Manual:** 1k warmup, then 100k iterations per scenario (same dataset, same process). Total time per scenario.
2. **BenchmarkDotNet:** Release build, `[MemoryDiagnoser]`, same dataset and templates. Reports mean time per call, error (99.9% CI), std dev, Gen0/Gen1 per 1k ops, and allocated memory per operation.

**Important:** BenchmarkDotNet runs each method in isolation (separate process and iterations), so we do not compare Flat vs 2D in a single process; we compare each scenario to its own baseline. The manual run compares all three in one process but has higher variance. Both are valid: BenchmarkDotNet for stable per-call and memory numbers, manual for a quick relative check.

### BenchmarkDotNet results (timing and memory)

`ResolutionBenchmarks` is annotated with `[MemoryDiagnoser]`. Method names in code: `V1_Flat_2Params`, `V2_1ExprPerPlaceholder`, `V2_5ExprPerPlaceholder`. Representative output (Release, single run):

| Method | Mean | Error (99.9% CI) | StdDev | Gen0 / 1k op | Gen1 / 1k op | Allocated / op |
|--------|------|-------------------|--------|----------------|----------------|-----------------|
| **Flat_2Params** | **14.06 µs** | 0.257 µs | 0.241 µs | 6.18 | 0.015 | **37.95 KB** |
| **2D_1ExprPerPlaceholder** | **14.24 µs** | 0.285 µs | 0.317 µs | 6.21 | 0.015 | **38.13 KB** |
| **2D_5ExprPerPlaceholder** | **13.40 µs** | 0.193 µs | 0.161 µs | 6.59 | — | **40.38 KB** |

- **Mean:** Average time per resolution call.  
- **Error:** Half-width of 99.9% confidence interval.  
- **StdDev:** Standard deviation of measured times.  
- **Gen0 / Gen1:** GC collections per 1,000 operations.  
- **Allocated:** Managed memory per single invocation (includes building the keyed dataset, resolution, and `string.Format`).

**Takeaway:** All three are in the **~13–14 µs** range with **~38–40 KB** allocated per call. Option 2 with 5 candidates per placeholder allocates slightly more but can measure a bit faster due to cheap null lookups and variance. There is no meaningful performance or memory advantage for either option.

### Why 2D isn’t slower (and can look slightly faster)

- Most cost is in **expression resolution** (parsing, keying dataset, record lookup, type/JSON handling), not in looping over the small inner list.
- **Short-circuit:** For 2D we stop at the first non-empty per placeholder; failed lookups are cheap.
- For “1 expression per placeholder”, Flat and 2D do the **same number** of resolutions; any difference is JIT/measurement noise.

So: **benchmarks do not justify choosing one option over the other for performance or memory.**

### How to run

- Full run (tests + manual benchmark + BenchmarkDotNet): `dotnet run -c Release`
- BenchmarkDotNet runs after the manual benchmark and prints the table above plus optional exports (see References).

---

## Decision

TBD.

---

## References

- Example rules: `Docs/WPTASK-mongo-example.json` (WPTASK = Option 1, WPTASK_2D = Option 2).
- Resolution: `TemplateEngine/TemplateEngine.cs` — `Resolve` (Option 1), `ResolveV2` (Option 2).
- Benchmarks: `Program.RunBenchmark` (manual), `ResolutionBenchmarks.cs` (BenchmarkDotNet, `[MemoryDiagnoser]`). For more precision: add `[NativeMemoryDiagnoser]` or use BenchmarkDotNet’s `DisassemblyDiagnoser` / export options.
