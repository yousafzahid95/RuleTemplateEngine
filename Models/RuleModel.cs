using System;
using System.Collections.Generic;

namespace RuleTemplateEngine.Models
{
    public class RuleModel
    {
        public string _id { get; set; }
        public string RuleName { get; set; }
        public List<string> Events { get; set; }
        public ActionItemTemplate ActionItemTemplate { get; set; }
        public Filters Filters { get; set; }
        public Checks Checks { get; set; }
    }

    public class ActionItemTemplate
    {
        public string Description { get; set; }
        public string TaskId { get; set; }
        public string EntityId { get; set; }
        public string SourceSystemKey { get; set; }
        public string ItemDefinitionGuid { get; set; }
        public int SourceSystem { get; set; }
    }

    public class Filters
    {
        public List<DataSourceConfig> DataSources { get; set; }
        public List<object> ValidationRules { get; set; }
    }

    public class Checks
    {
        public List<DataSourceConfig> DataSources { get; set; }
        public List<object> ValidationRules { get; set; }
    }

    public class DataSourceConfig
    {
        public string Key { get; set; }
        public Dictionary<string, string> DataSourceParams { get; set; }
    }
}
