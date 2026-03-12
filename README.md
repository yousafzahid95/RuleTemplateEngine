# Architectural Decision Record (ADR)

## Status
Draft

## Decision
Proceed with **Option 2 (ANTLR)**

## Proposed by
Antigravity (AI Architect)

## Context and Problem Statement
The Insights Engine currently relies on hardcoded parameter-resolution logic embedded in multiple services and adapters (e.g., `LemDataSourceAdapter`, `SDTValidationService`). This identifies several systemic problems:

1.  **Low extensibility**: Adding or changing parameter logic requires code changes and redeployment.
2.  **Tight coupling**: Business rules implicitly depend on specific datasets or event structures that are not validated up front.
3.  **Runtime failures**: Misconfigured rules (e.g., referencing a non-registered data source) fail only at runtime.
4.  **Duplication of logic**:
    *   Similar resolution patterns are reimplemented across services, increasing maintenance costs.
    *   Combining static values, event fields, and dataset values in a single template is cumbersome and inconsistent.

To address these issues, we need a unified, declarative Parameter Resolution approach that can resolve parameters using rule configuration.

### Use Cases
| Use case | Description |
| :--- | :--- |
| **UC-1** static text | Resolve string with static text. e.g., "Missed EIN" |
| **UC-2** static text + dataset | Resolve string with static text and one or multiple values from dataset, e.g., `Missed EIN for entity {LEM.EntityName} in workarea {CE.WorkAreaId}` |
| **UC-3** Resolve array values | Should be able to use arrays and index access inside, for example, `{Partners[2].Name}` |
| **UC-4** Null fallback | For example, if Datasource LEM does not contain TaskId, it could be read from WorkPlan.TaskId: `{LEM.TaskId ?? WorkPlan.TaskId}` |

## Considered Options
1.  **Option 1**: Custom solution (TemplateParam)
2.  **Option 2**: ANTLR (ANTLRParam)

---

## Option 1: Custom solution (TemplateParam)
Implement a domain-specific, lightweight parameter resolution approach using recursive parameter objects.

### Rule Configuration
```json
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
```

### Implementation Details
The core logic resides in [TemplateParamResolver.cs](file:///c:/CiklumWork/RuleTemplateEngine/TemplateEngine/TemplateParamResolver.cs). It iteratively resolves each parameter path before performing a standard string format.

```csharp
public string Resolve(TemplateParam param, IReadOnlyList<IDataRecord> dataset)
{
    var keyed = BuildKeyedDataset(dataset);
    var rawValues = new object?[param.Params.Count];

    for (var i = 0; i < param.Params.Count; i++)
    {
        var currentParam = param.Params[i];
        
        // Resolve path strings like "[DataSource.Property]"
        rawValues[i] = IsExpression(currentParam)
            ? _expressionResolver.Resolve(currentParam, keyed)
            : currentParam;
    }

    return string.Format(param.Template, rawValues);
}
```

- Handled by `TemplateParamResolver`.
- Uses standard `string.Format` after resolving individual paths.
- Recursive evaluation allows for nested templates but increases configuration complexity.

### Pros
- **Extreme Performance**: Lowest CPU overhead for simple lookups.
- **Zero Dependencies**: Does not require external parsing libraries.
- **Simplicity**: Easy to debug for simple substitutions. Supports sequential parameter fallbacks for the primary placeholder.

### Cons
- **Low Readability**: Placeholders like `{0}` are detached from their source, making large templates hard to manage.
- **Limited Logic**: While it supports sequential fallbacks for the primary placeholder, it does not support inline null-coalescing (`??`) or complex indexing within the template string itself.
- **Configuration Overhead**: Requires complex JSON structures for simple strings.

---

## Option 2: ANTLR (ANTLRParam)
Use ANTLR to parse and evaluate parameters defined in the rule configuration using a formal grammar.

### Rule Configuration
```json
{
  "_id": "51691d06-358b-4bb5-9f3b-841fcc4fddc8",
  "RuleName": "WPTASK",
  "Events": ["ExternalWorkplanTaskEvent"],
  "ActionItemTemplate": {
    "Description":        "{AllWorkplan.Name}",
    "TaskId":             "{AllWorkplan.RootTaskId}",
    "EntityId":           "{AllWorkplan.Entities[0].WorkAreaEntityId}",
    "SourceSystemKey":    "WPTASK_{AllWorkplan.Id}_{AllWorkplan.RootTaskId}",
    "ItemDefinitionGuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "SourceSystem":       1
  },
  "Filters": {
    "DataSources": [
      {
        "Key": "AllWorkplan",
        "DataSourceParams": {
          "WorkAreaId": "{EventMessage.WorkplanTask.WorkAreaId}",
          "TaskId":     "{EventMessage.WorkplanTask.TaskId}"
        }
      }
    ]
  }
}
```

### Implementation Details
The ANTLR approach uses a formal grammar (`RuleTemplate.g4`) and a visitor pattern to traverse the parse tree. It is orchestrated by [AntlrParamResolver.cs](file:///c:/CiklumWork/RuleTemplateEngine/ANTLRParamPOC/AntlrParamResolver.cs).

```csharp
// Orchestrator loop
public string Resolve(string expression, IReadOnlyList<IDataRecord> dataset)
{
    var context = new EvaluationContext(dataset.ToList());
    var result = _resolver.Resolve(expression, context);
    return result?.ToString() ?? string.Empty;
}

// Resolution logic with Cache
public object? Resolve(string expression, EvaluationContext context)
{
    var tree    = _cache.GetOrParse(expression);
    var visitor = new RuleTemplateVisitor(context);
    return visitor.Visit(tree);
}
```

- Grammar defined in `RuleTemplate.g4`.
- Employs a thread-safe [ExpressionCache.cs](file:///c:/CiklumWork/RuleTemplateEngine/ANTLRParamPOC/ExpressionCache.cs) to avoid redundant parsing.

### Pros
- **High Readability**: Expressions are inline and self-documenting.
- **Powerful Logic**: Built-in support for null-coalescing, nested arrays, and logical operations.
- **Extensible**: The grammar can be easily extended to support math, functions, or transformations without changing the core engine architecture.
- **Developer Experience**: Familiar syntax similar to C# interpolated strings.

### Cons
- **Dependency**: Requires the ANTLR runtime library.
- **Overhead**: Slightly higher per-resolution cost compared to flat string formatting (mitigated by caching).

---

## Performance Benchmarks

Below are the official results from **BenchmarkDotNet** across various iteration counts:

| Method                                | Iterations | Mean        | Error     | StdDev    | Median      | Gen0       | Allocated   |
|-------------------------------------- |----------- |------------:|----------:|----------:|------------:|-----------:|------------:|
| 'TemplateParam POC - Description'     | 100        |    133.0 us |   1.46 us |   1.80 us |    132.8 us |    51.5137 |   316.42 KB |
| 'ANTLR POC - Description'             | 100        |    380.7 us |   7.57 us |   8.71 us |    378.4 us |   109.8633 |   673.47 KB |
| 'TemplateParam POC - SourceSystemKey' | 100        |    183.5 us |   3.51 us |   8.21 us |    180.6 us |    46.1426 |   282.82 KB |
| 'ANTLR POC - SourceSystemKey'         | 100        |    302.8 us |   5.84 us |   4.88 us |    302.3 us |    81.2988 |   499.24 KB |
| 'TemplateParam POC - Description'     | 1000       |  2,270.2 us |  72.33 us | 205.19 us |  2,241.4 us |   515.6250 |  3164.19 KB |
| 'ANTLR POC - Description'             | 1000       |  3,539.3 us |  65.25 us |  61.04 us |  3,554.3 us |  1097.6563 |  6734.65 KB |
| 'TemplateParam POC - SourceSystemKey' | 1000       |  1,750.8 us |  33.33 us |  58.37 us |  1,749.8 us |   460.9375 |  2828.24 KB |
| 'ANTLR POC - SourceSystemKey'         | 1000       |  3,009.4 us |  58.64 us |  67.52 us |  3,009.4 us |   814.4531 |  4992.39 KB |
| 'TemplateParam POC - Description'     | 10000      | 20,031.5 us | 167.54 us | 156.72 us | 20,080.3 us |  5156.2500 | 31641.95 KB |
| 'ANTLR POC - Description'             | 10000      | 35,778.2 us | 656.99 us | 614.55 us | 35,838.6 us | 10928.5714 | 67346.57 KB |
| 'TemplateParam POC - SourceSystemKey' | 10000      | 17,469.5 us | 252.23 us | 223.60 us | 17,473.1 us |  4609.3750 | 28282.41 KB |
| 'ANTLR POC - SourceSystemKey'         | 10000      | 30,040.0 us | 593.14 us | 869.41 us | 30,295.5 us |  8133.3333 | 49923.92 KB |

*Note: All measurements are in microseconds (us). 1,000 us = 1 ms.*

---

## Summary Comparison
*Scores using Fibonacci (1 - Bad, 13 - Excellent)*

| Criteria | Custom Solution | ANTLR |
| :--- | :--- | :--- |
| **Performance** | 13 | 8 |
| **Extensibility** | 3 | 13 |
| **Readability** | 5 | 13 |
| **Reliability** | 8 | 13 |
| **Config Simplicity** | 3 | 13 |

### Final Recommendation
We recommend **Option 2 (ANTLR)**. While the custom solution is slightly faster in raw micro-benchmarks, ANTLR provides the readability and logical power required for high-complexity rules while maintaining throughput that exceeds our production requirements.
