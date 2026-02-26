namespace RuleTemplateEngine.Dtos
{
    public class EntityWorkAreaLevelDetailIntegrationDto : BaseEntityWorkAreaLevelIntegrationDto
    {
        /// <summary>Alias for Id for template expressions like [LEM.EntityId].</summary>
        public Guid EntityId { get => Id; set => Id = value; }

        public IList<TaxPeriodResponseIntegrationDto> TaxPeriods { get; set; } = new List<TaxPeriodResponseIntegrationDto>();

        public Guid? SourceId { get; set; }

        public Guid? SourceOperationId { get; set; }

        public SourceOperationIntegrationType? SourceOperationType { get; set; }

        public Guid CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public IList<IntegrationEntityCollectionShortDto> EntityCollections { get; set; } = new List<IntegrationEntityCollectionShortDto>();

        public bool IsReadOnly { get; set; }

        public Guid? SyncedFrom { get; set; }
    }

    public class TaxPeriodResponseIntegrationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public enum SourceOperationIntegrationType
    {
        None = 0,
        Create = 1,
        Update = 2
    }

    public class IntegrationEntityCollectionShortDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
