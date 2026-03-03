# ADR: Rule template params — 1D vs 2D array

## Status

TBD.

## Context

Rule templates resolve placeholders (`{0}`, `{1}`, …) from a dataset using expressions like `[Workplan.Id]` or `[EventMessage.WorkplanTask.WorkAreaId]`. Each placeholder can be bound to:

- **One expression** (e.g. `{0}` ← `[Workplan.Id]`), or  
- **Multiple fallback expressions** (e.g. `{0}` ← first non-empty among `[Workplan.Id]`, `[EventData.WorkplanTask.Id]`).

We need a consistent way to represent this in rule JSON and in the resolution engine. Two approaches are used in the codebase:

1. **1D params** — `params` is a flat list of strings; `params[i]` maps to `{i}`. For a single placeholder with fallbacks, the engine treats multiple entries as “first non-empty” only when the template is exactly `"{0}"`.
2. **2D params** — `params` is a list of lists; `params[i]` is the list of candidate expressions for `{i}`. The engine tries each candidate in order and uses the first non-empty value per placeholder.

This ADR documents both formats, their pros/cons, and how benchmarks inform the choice.

## Exemplary rules

The repo includes two Mongo-style rule examples that are semantically equivalent but use the two param formats.

### 1D params — `WPTASK` (flat list)

- **RuleName**: `WPTASK`
- **File**: `Docs/A002IR-mongo-example.json` (first document)

**ActionItemTemplateDefinition** (excerpt):

```json
{
  "Description": {
    "Params": ["[Workplan.Name]"],
    "Template": "{0}"
  },
  "SourceSystemKey": {
    "Params": ["[Workplan.Id]"],
    "Template": "WPTASK_{0}"
  }
}
```

- One placeholder: one string in `Params`. Multiple placeholders: one string per slot, e.g. `["[LEM.Name]", "[Event.ProjectId]"]` for `"Review {0} for project {1}"`.
- Fallbacks for a single `{0}` are represented by multiple entries in the same flat list; the engine uses the first non-empty only when `Template` is `"{0}"`.

### 2D params — `WPTASK_2D` (list of lists)

- **RuleName**: `WPTASK_2D`
- **File**: `Docs/A002IR-mongo-example.json` (second document)

**ActionItemTemplateDefinition** (excerpt):

```json
{
  "Description": {
    "Params": [["[Workplan.Name]"]],
    "Template": "{0}"
  },
  "SourceSystemKey": {
    "Params": [
      ["[Workplan.Id]", "[EventData.WorkplanTask.Id]"],
      ["[EventData.WorkplanTask.WorkAreaId]", "[EventData.WorkplanTask.WorkareaId]"]
    ],
    "Template": "WPTASK_{0}_{1}"
  }
}
```

- Each placeholder has its own list of candidates. For `{0}` and `{1}`, `Params` has two inner arrays; the engine tries each list in order and uses the first non-empty per placeholder.
- Fallbacks are explicit and work for any template, not only `"{0}"`.

## Pros and cons

### 1D params (flat list)

| Pros | Cons |
|------|------|
| Simple mental model: `params[i]` → `{i}`. | Fallback semantics only apply when template is exactly `"{0}"`; otherwise multiple params for one placeholder are ambiguous. |
| Compact JSON and easy to author for “one expression per placeholder”. | Hard to express “first non-empty for `{0}`” in a template like `"WPTASK_{0}_{1}"` where `{0}` has fallbacks. |
| Single type: `List<string>`. Easy to validate and serialize. | Mixing “one value per slot” and “fallbacks for one slot” in the same list can be confusing. |

### 2D params (list of lists)

| Pros | Cons |
|------|------|
| Clear semantics: `params[i]` = list of candidates for `{i}`; first non-empty wins. | Slightly more verbose JSON (extra brackets). |
| Fallbacks work for every placeholder and any template. | Two shapes to support if the system accepts both 1D and 2D. |
| Self-describing: structure reflects “try these in order”. | Iteration over inner lists is explicit; with many fallbacks, more resolutions are attempted until one succeeds. |

## Benchmarks and why 1D vs 2D resolution don’t differ meaningfully

The project measures resolution performance with two harnesses:

| Harness | Purpose |
|--------|---------|
| **Manual** (`Program.RunBenchmark`) | 100k iterations, ~50 LEM records; raw total time per scenario. |
| **BenchmarkDotNet** (`ResolutionBenchmarks`) | Release build, same dataset; per-call time and memory with warmup/JIT and statistics. |

**How to run:** `dotnet run -c Release` (runs tests, manual benchmark, then BenchmarkDotNet). For BenchmarkDotNet only: run the app and let it complete the “BenchmarkDotNet” section; or use a dedicated benchmark runner.

---

### Benchmark setup

- **Dataset:** 50 records (3 fixed LEM DTOs + 47 generated), transformed to `IDataRecord` with prefix `"LEM"`.
- **Template:** `"A002IR_{0}_{1}"` with two placeholders.
- **Scenarios:**
  - **V1 flat:** 1D `Params = ["[LEM.EntityId]", "[LEM.WorkAreaId]"]` — two expressions, one per placeholder.
  - **V2 1 expr/placeholder:** 2D `Params = [["[LEM.EntityId]"], ["[LEM.WorkAreaId]"]]` — same expressions, one candidate per placeholder.
  - **V2 5 exprs/placeholder:** 2D with two inner lists of 5 candidates each (4 missing paths + 1 hit); exercises fallback iteration.

---

### BenchmarkDotNet results (timing and memory)

`ResolutionBenchmarks` is annotated with `[MemoryDiagnoser]`. Representative output (Release, single run):

| Method | Mean | Error (99.9% CI) | StdDev | Gen0 / 1k op | Gen1 / 1k op | Allocated / op |
|--------|------|-------------------|--------|----------------|----------------|-----------------|
| **V1_Flat_2Params** | **14.06 µs** | 0.257 µs | 0.241 µs | 6.18 | 0.015 | **37.95 KB** |
| **V2_1ExprPerPlaceholder** | **14.24 µs** | 0.285 µs | 0.317 µs | 6.21 | 0.015 | **38.13 KB** |
| **V2_5ExprPerPlaceholder** | **13.40 µs** | 0.193 µs | 0.161 µs | 6.59 | — | **40.38 KB** |

- **Mean:** Average time per resolution call.  
- **Error:** Half-width of 99.9% confidence interval.  
- **StdDev:** Standard deviation of measured times.  
- **Gen0 / Gen1:** GC collections per 1,000 operations (Gen1 “—” = negligible).  
- **Allocated:** Managed memory allocated per single invocation (includes `BuildKeyedDataset`, resolution, and `string.Format`).

**Takeaway:** All three are in the **~13–14 µs** band. Allocations are **~38–40 KB** per call (dominated by keyed dataset building and resolution). 2D with 5 candidates per placeholder allocates slightly more but can measure a bit faster due to cheap null lookups and variance.

---

### Manual benchmark (100k iterations, total time)

Typical range on the same dataset:

| Scenario | Total time (100k iter) | vs V1 flat |
|----------|------------------------|------------|
| V1 flat (2 params) | ~2.0–2.5 s | baseline |
| V2 1 expr/placeholder (2×1) | ~1.8–2.2 s | similar / slightly lower |
| V2 5 expr/placeholder (2×5) | ~1.8–2.0 s | similar / slightly lower |

Run-to-run variation is significant; use BenchmarkDotNet for stable, comparable numbers.

---

### Why 2D doesn’t have to be slower (and can look faster)

- **Cost is dominated by expression resolution**, not by “looping the 2D array”:
  - Parsing `[Source.Key.Property]`, grouping the dataset by key, indexing into the record, and (in this implementation) JSON deserialization / type lookup dominate.
  - The extra loop over the inner list is a small number of iterations (e.g. 1 or 5) and is cheap compared to a single `ExpressionResolver.Resolve` call.
- **Short-circuit on first hit:** For 2D, we stop at the first non-empty value per placeholder. For “5 candidates, 4 null + 1 hit”, we do 4 fast null lookups and 1 full resolve. The misses are cheap, so total time can be similar or slightly better in a given run.
- **No algorithmic advantage for 1D:** For “1 expression per placeholder”, 1D and 2D do the same number of resolutions. Any timing gap is attributable to code layout, inlining, or measurement noise.

So: **benchmarks show that 1D vs 2D resolution do not make a meaningful performance or memory difference**; the 2D version is not slower despite iterating over the 2D array, and it can occasionally appear a bit faster due to cheap failed lookups and normal variance.

## Decision

TBD.

## References

- Example rules: `Docs/A002IR-mongo-example.json` (WPTASK, WPTASK_2D).
- Resolution: `TemplateEngine/TemplateEngine.cs` (`Resolve` for 1D, `ResolveV2` for 2D).
- Benchmarks: `Program.RunBenchmark`, `ResolutionBenchmarks.cs` (includes `[MemoryDiagnoser]`). For more precision: add `[NativeMemoryDiagnoser]` or use BenchmarkDotNet’s `DisassemblyDiagnoser` / export options.
