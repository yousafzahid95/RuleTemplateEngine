using Newtonsoft.Json;

namespace RuleTemplateEngine.Models
{
    /// <summary>
    /// Root rule definition with events, action item template, and data source checks.
    /// </summary>
    public class RuleTemplate
    {
        /// <summary>MongoDB document id (optional; set when loading from DB).</summary>
        [JsonProperty("_id")]
        public string? Id { get; set; }

        [JsonProperty("RuleName")]
        public string RuleName { get; set; } = string.Empty;

        [JsonProperty("Events")]
        public List<string> Events { get; set; } = new();

        [JsonProperty("ActionItemTemplate")]
        public ActionItemTemplateDefinition ActionItemTemplate { get; set; } = new();

        /// <summary>Data source definitions (e.g. LEM params). Stored under "Checks" in MongoDB.</summary>
        [JsonProperty("Checks")]
        public RuleChecks? Checks { get; set; }
    }

    /// <summary>
    /// Action item output template: each property has a format template and param expressions.
    /// </summary>
    public class ActionItemTemplateDefinition
    {
        [JsonProperty("SourceSystem")]
        public int SourceSystem { get; set; }

        [JsonProperty("ItemDefinitionId")]
        public int ItemDefinitionId { get; set; }

        [JsonProperty("Description")]
        public TemplateParam? Description { get; set; }

        [JsonProperty("TaskId")]
        public TemplateParam? TaskId { get; set; }

        [JsonProperty("EntityId")]
        public TemplateParam? EntityId { get; set; }

        [JsonProperty("SourceSystemKey")]
        public TemplateParam? SourceSystemKey { get; set; }
    }

    /// <summary>
    /// A single template: format string (e.g. "{0}", "Review {0} for project {1}") and 2D param expressions.
    /// Params[i] is a list of fallback expressions for placeholder {i}.
    /// For each placeholder, expressions are tried in order; the first non-empty resolved value wins.
    /// Resolved via String.Format(template, ...resolvedParams).
    /// </summary>
    public class TemplateParam
    {
        [JsonProperty("template")]
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// 2D list: Params[0] = candidates for {0}, Params[1] = candidates for {1}, etc.
        /// Each inner list is tried in order; first non-empty resolved value is used.
        /// </summary>
        [JsonProperty("params")]
        public List<List<string>> Params { get; set; } = new();
    }

    /// <summary>
    /// Checks section containing data source definitions.
    /// </summary>
    public class RuleChecks
    {
        [JsonProperty("DataSources")]
        public List<DataSourceDefinition> DataSources { get; set; } = new();
    }

    /// <summary>
    /// One data source: Key (e.g. "AllWorkplan") and params as template/params resolved from event.
    /// Adapter receives IDictionary&lt;string, object&gt; built from resolved params.
    /// </summary>
    public class DataSourceDefinition
    {
        [JsonProperty("Key")]
        public string Key { get; set; } = string.Empty;

        [JsonProperty("params")]
        public Dictionary<string, TemplateParam> Params { get; set; } = new();
    }
}
