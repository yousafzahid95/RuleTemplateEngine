using BenchmarkDotNet.Running;

var commandLineArgs = Environment.GetCommandLineArgs();

// When invoked with BenchmarkDotNet filter targeting PocComparisonBenchmarks,
// run only the benchmarks (skip interactive POC runs).
if (commandLineArgs.Any(a => a.Contains("PocComparisonBenchmarks", StringComparison.OrdinalIgnoreCase)))
{
    BenchmarkRunner.Run<PocComparisonBenchmarks>();
    return;
}

Console.WriteLine("Starting Both Proof of Concepts...");
Console.WriteLine("\n\n");

// 1. ANTLRParam Evaluation POC
await RuleTemplateEngine.ANTLRParamPOC.POCRunner.Run();

Console.WriteLine("\n\n");

// 2. TemplateParam-based POC
await RuleTemplateEngine.TemplateParamPOC.TemplateParamPOCRunner.Run();

Console.WriteLine("\n\nAll POC evaluations completed successfully.");
