using System;
using System.Collections.Generic;

namespace RuleTemplateEngine.Dtos
{
    public class FullTaskDTO : LiteTaskDTO
    {
        public string Description { get; set; }
        public string RootTaskName { get; set; }
        public bool HasChildren { get; set; }
        public Guid? CompletedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public Guid CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
        public Guid? UpdatedBy { get; set; }
        public string UpdatedByName { get; set; }
    }

    public class EntityInfoDTO
    {
        public Guid WorkAreaEntityId { get; set; }
    }

    public class LiteTaskDTO
    {
        public Guid Id { get; set; }
        public Guid TaskTypeTemplateId { get; set; }
        public string TaskTypeTemplateName { get; set; }
        public int Status { get; set; }
        public string Name { get; set; }
        public Guid WorkAreaId { get; set; }
        public string WorkAreaName { get; set; }
        public string ProjectName { get; set; }
        public string ClientName { get; set; }
        public Guid RootTaskId { get; set; }
        public Guid ProjectId { get; set; }
        public Guid? ClientId { get; set; }
        public string Path { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsInRecycleBin { get; set; }
        public DateTimeOffset? DueDate { get; set; }
        public string ObligationType { get; set; }
        public EntityInfoDTO[] Entities { get; set; }
    }
}
