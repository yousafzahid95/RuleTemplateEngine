using Microsoft.Extensions.DependencyInjection;
using RuleTemplateEngine.ANTLRParamPOC;
using RuleTemplateEngine.TemplateEngine;
using RuleTemplateEngine.TemplateParamPOC;

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
services.AddTransient<RuleTemplateEngine.Benchmarks.BatchPerformanceRunner>();

var serviceProvider = services.BuildServiceProvider();

var commandLineArgs = Environment.GetCommandLineArgs();

// Note: PocComparisonBenchmarks still uses static-style or needs its own DI setup if we refactor it too.
// For now, let's keep the interactive POC runners clean.

Console.WriteLine("Starting Both Proof of Concepts (DI Refactored)...");
Console.WriteLine("\n\n");

// 1. ANTLRParam Evaluation POC
var antlrRunner = serviceProvider.GetRequiredService<RuleTemplateEngine.ANTLRParamPOC.POCRunner>();
await antlrRunner.Run();

Console.WriteLine("\n\n");

// 2. TemplateParam-based POC
var templateRunner = serviceProvider.GetRequiredService<RuleTemplateEngine.TemplateParamPOC.TemplateParamPOCRunner>();
await templateRunner.Run();

Console.WriteLine("\n\n");

// 3. Performance Batch Comparison
var batchRunner = serviceProvider.GetRequiredService<RuleTemplateEngine.Benchmarks.BatchPerformanceRunner>();
batchRunner.RunAll();

Console.WriteLine("\n\nAll POC evaluations completed successfully.");
