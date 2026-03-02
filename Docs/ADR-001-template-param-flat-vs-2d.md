# ADR-001: TemplateParam — Flat List vs 2D List for Placeholder Resolution

**Status:** Accepted
**Date:** 2026-03-02
**Context:** RuleTemplateEngine placeholder resolution strategy

---

## Context

`TemplateParam` resolves format strings like `"A002IR_{0}_{1}"` by mapping each `{i}` placeholder to a value looked up from the dataset.

We have two approaches for structuring the param expressions:

| | V1 (Flat) | V2 (2D) |
|---|---|---|
| Type | `List<string>` | `List<List<string>>` |
| Mapping | `Params[i]` is a single expression for `{i}` | `Params[i]` is a list of fallback expressions for `{i}` |
| Fallback | Special-case: template `"{0}"` with multiple params tries each as fallback | Built-in: each inner list is tried in order per placeholder |

### V1 — Flat (`TemplateParam`)

```csharp
SourceSystemKey = new TemplateParam
{
    Template = "A002IR_{0}_{1}",
    Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
}
```

Fallback only works with a single `{0}` placeholder — all params become fallback candidates for that one slot:

```csharp
EntityId = new TemplateParam
{
    Template = "{0}",
    Params = { "[Event.EntityIds[0]]", "[Event.Entities[0].WorkAreaEntityId]", "[Event.EntityId]" }
}
```

### V2 — 2D (`TemplateParamV2`)

```csharp
SourceSystemKey = new TemplateParamV2
{
    Template = "A002IR_{0}_{1}",
    Params = new List<List<string>>
    {
        new() { "[LEM.EntityId]" },
        new() { "[LEM.WorkAreaId]" }
    }
}
```

Each placeholder gets its own independent fallback chain:

```csharp
Description = new TemplateParamV2
{
    Template = "Review {0} for project {1}",
    Params = new List<List<string>>
    {
        new() { "[LEM.MissingName]", "[LEM.Name]", "[LEM.EntityId]" },  // {0} fallbacks
        new() { "[Event.ProjectName]", "[Event.ProjectId]" }             // {1} fallbacks
    }
}
```

---

## Decision

Keep both `TemplateParam` (V1) and `TemplateParamV2` (V2) available. Use V2 for new rule definitions going forward.

---

## Pros and Cons

### V1 — Flat `List<string>`

#### Pros
- **Simpler model** — a flat list is easy to read, write, and serialize
- **Less nesting** — JSON representation is compact (`"params": ["[LEM.EntityId]", "[LEM.WorkAreaId]"]`)
- **Familiar** — maps directly to `String.Format` positional args
- **Sufficient for simple cases** — when each placeholder has exactly one expression, the flat list works perfectly

#### Cons
- **Fallback is a special case** — only works when template is exactly `"{0}"` with multiple params; not composable
- **Cannot fallback per placeholder** — template `"A002IR_{0}_{1}"` with 2 placeholders cannot have fallback candidates per slot
- **Ambiguous semantics** — `Params = { "[A]", "[B]", "[C]" }` could mean 3 placeholders or 3 fallbacks for `{0}`, depending on the template
- **Performance overhead** — the special-case `{0}` detection path uses LINQ (`FirstOrDefault`), adding allocation pressure in hot loops

### V2 — 2D `List<List<string>>`

#### Pros
- **Per-placeholder fallback** — each `{i}` has its own independent fallback chain; no ambiguity
- **No special cases** — resolution logic is uniform: iterate candidates, take first non-empty
- **Composable** — works with any template, any number of placeholders, any depth of fallbacks
- **Faster** — simpler code path, no LINQ allocations, early `break` on first hit
- **Explicit intent** — the structure makes it clear which expressions target which placeholder

#### Cons
- **More verbose model** — 2D list requires more nesting in code and JSON
- **Slightly larger JSON** — `"params": [["[LEM.EntityId]"], ["[LEM.WorkAreaId]"]]` vs `"params": ["[LEM.EntityId]", "[LEM.WorkAreaId]"]`
- **Overkill for simple cases** — when each placeholder has exactly one expression, the inner list is a single-element wrapper
- **Migration cost** — existing rule definitions using `TemplateParam` need conversion or both models must coexist

---

## Benchmark Results

100,000 iterations, Release build (.NET 10):

| Scenario | Time | vs V1 |
|---|---|---|
| V1 flat (2 params, direct hit) | 637 ms | baseline |
| V2 1 expr/placeholder (2x1) | 206 ms | **3.1x faster** |
| V2 5 expr/placeholder (2x5, 4 nulls + 1 hit) | 332 ms | **1.9x faster** |

V2 is faster in all scenarios. Even the worst case (4 null lookups before hitting a value per placeholder) is nearly 2x faster than V1's direct-hit path.

---

## Consequences

- New rules should use `TemplateParamV2` with `ResolveV2`
- Existing rules using `TemplateParam` continue to work unchanged via `Resolve`
- The engine exposes both `Resolve` and `ResolveV2` — no breaking changes
- Future: consider a unified `Resolve` overload that accepts either type, or migrate V1 definitions to V2 format
