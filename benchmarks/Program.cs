using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Snail.MCP.Playwright.Benchmarks.PageBenchmarks).Assembly).Run(args);
