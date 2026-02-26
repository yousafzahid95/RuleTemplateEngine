namespace RuleTemplateEngine.Dtos
{
    public class BaseEntityWorkAreaLevelIntegrationDto
    {
        public Guid Id { get; set; }

        public Guid ProjectEntityId { get; set; }

        public string IntelaId { get; set; } = string.Empty;

        public Guid? CountryId { get; set; }

        public string CountryName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Guid? TypeId { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public string ExternalId { get; set; } = string.Empty;

        public Guid ProjectId { get; set; }

        public Guid WorkAreaId { get; set; }

        public Guid? TaxClassificationTypeId { get; set; }

        public string TaxClassificationTypeName { get; set; } = string.Empty;

        public IList<TemplateAttributeResponseIntegrationDto> Attributes { get; set; } = new List<TemplateAttributeResponseIntegrationDto>();

        public string Status { get; set; } = string.Empty;

        public Guid? ClonedFrom { get; set; }

        public string ActualName { get; set; } = string.Empty;

        public VisibilityDto? Visibility { get; set; }

        public DateTime? LastModifiedDate { get; set; }
    }

    public class TemplateAttributeResponseIntegrationDto
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    public class VisibilityDto
    {
        public bool IsVisible { get; set; }
    }
}
