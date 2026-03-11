using BenchmarkDotNet.Attributes;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;

[MemoryDiagnoser]
public class ResolutionBenchmarks
{
    private IReadOnlyList<IDataRecord> _dataset = null!;
    private TemplateParam _v1Direct = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Same dataset and templates as RunBenchmark to make results comparable
        var lemDtos = new List<EntityWorkAreaLevelDetailIntegrationDto>
        {
            new() { Id = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("aaaaaaaa-3333-3333-3333-333333333333") },
            new() { Id = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333") },
            new() { Id = Guid.Parse("cccccccc-1111-1111-1111-111111111111"), WorkAreaId = Guid.Parse("cccccccc-2222-2222-2222-222222222222"), ProjectId = Guid.Parse("cccccccc-3333-3333-3333-333333333333") }
        };
        for (var i = 0; i < 47; i++)
        {
            lemDtos.Add(new EntityWorkAreaLevelDetailIntegrationDto
            {
                Id = Guid.NewGuid(),
                WorkAreaId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            });
        }

        var lemRecords = TransformToIDataRecord<EntityWorkAreaLevelDetailIntegrationDto>.TransformFromList(lemDtos, "LEM").ToList();
        _dataset = new List<IDataRecord>(lemRecords);

        _v1Direct = new TemplateParam
        {
            Template = "A002IR_{0}_{1}",
            Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
        };

       
    }

    [Benchmark]
    public string V1_Flat_2Params() =>
        RuleTemplateEngine.TemplateEngine.RuleTemplateEngine.Resolve(_v1Direct, _dataset);

}

