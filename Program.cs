using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;
using RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.TemplateEngine;

var services = new ServiceCollection();

// Core dependencies
services.AddSingleton<RuleTemplateEngine.TemplateEngine.IExpressionResolver, RuleTemplateEngine.TemplateEngine.ExpressionResolver>();
services.AddSingleton<ITemplateParamResolver, TemplateParamResolver>();

// ANTLR dependencies
services.AddSingleton<IExpressionCache, ExpressionCache>();
services.AddSingleton<IAntlrExpressionResolver, RuleTemplateEngine.ANTLRParamPOC.ExpressionResolver>();
services.AddSingleton<IAntlrParamResolver, AntlrParamResolver>();

// Mock Runners
services.AddTransient<RuleTemplateEngine.ANTLRParamPOC.POCRunner>();
services.AddTransient<RuleTemplateEngine.TemplateParamPOC.TemplateParamPOCRunner>();

var serviceProvider = services.BuildServiceProvider();

// Check for benchmark flag
if (args.Contains("--benchmark"))
{
    Console.WriteLine("Starting Professional Benchmark Analysis (BenchmarkDotNet)...");
    BenchmarkRunner.Run<PocComparisonBenchmarks>();
    return;
}

Console.WriteLine("Starting Both Proof of Concepts (DI Refactored)...");
Console.WriteLine("\n\n");

// 1. ANTLRParam Evaluation POC
var antlrRunner = serviceProvider.GetRequiredService<RuleTemplateEngine.ANTLRParamPOC.POCRunner>();
await antlrRunner.Run();

Console.WriteLine("\n\n");

// 2. TemplateParam-based POC
var templateRunner = serviceProvider.GetRequiredService<RuleTemplateEngine.TemplateParamPOC.TemplateParamPOCRunner>();
await templateRunner.Run();

Console.WriteLine("\n\nAll POC evaluations completed successfully.");
Console.WriteLine("Note: Run with '--benchmark' for iteration performance comparison.");
