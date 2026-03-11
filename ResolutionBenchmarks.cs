using BenchmarkDotNet.Attributes;
using RuleTemplateEngine.Dtos;
using RuleTemplateEngine.Helpers;
using RuleTemplateEngine.Interfaces;
using RuleTemplateEngine.Models;
using RuleTemplateEngine.TemplateEngine;

[MemoryDiagnoser]
public class ResolutionBenchmarks
{
    private IReadOnlyList<IDataRecord> _dataset = null!;
    private TemplateParam _v1Direct = null!;
    private IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> _keyed = null!;
    private string _exprFirst = null!;
    private string _exprMiddle = null!;
    private string _exprIndexed = null!;
    private IExpressionResolver _exprResolver = null!;
    private TemplateParamResolver _templateResolver = null!;

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
        _exprResolver = new ExpressionResolver();
        _templateResolver = new TemplateParamResolver(_exprResolver);
        _keyed = TemplateParamResolver.BuildKeyedDataset(_dataset);

        _v1Direct = new TemplateParam
        {
            Template = "A002IR_{0}_{1}",
            Params = { "[LEM.EntityId]", "[LEM.WorkAreaId]" }
        };
        _exprFirst = "[LEM.EntityId]";
        _exprMiddle = "[LEM.WorkAreaId]";
        _exprIndexed = "[LEM[10].ProjectId]";
       
    }

    [Benchmark]
    public string V1_Flat_2Params() =>
        _templateResolver.Resolve(_v1Direct, _dataset);

    [Benchmark(Description = "String resolver - simple path")]
    public object? StringResolver_Simple() =>
        _exprResolver.Resolve(_exprFirst, _keyed);

    // [Benchmark(Description = "ANTLR resolver - simple path")]
    // public object? AntlrResolver_Simple() =>
    //     ExpressionResolverAntlr.Resolve(_exprFirst, _keyed);

    [Benchmark(Description = "String resolver - indexed path")]
    public object? StringResolver_Indexed() =>
        _exprResolver.Resolve(_exprIndexed, _keyed);

    // [Benchmark(Description = "ANTLR resolver - indexed path")]
    // public object? AntlrResolver_Indexed() =>
    //     ExpressionResolverAntlr.Resolve(_exprIndexed, _keyed);

}

