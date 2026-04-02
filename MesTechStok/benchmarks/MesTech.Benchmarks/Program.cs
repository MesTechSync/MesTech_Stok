using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace MesTech.Benchmarks;

/// <summary>
/// MesTech Benchmark Runner — BenchmarkDotNet ile mikro-benchmark.
/// Kullanim: dotnet run -c Release --project benchmarks/MesTech.Benchmarks/
/// Tum benchmark'lar: dotnet run -c Release -- --filter *
/// Tek benchmark: dotnet run -c Release -- --filter *HandlerBenchmarks*
/// BuildTimeout: 5 dk (buyuk proje — 4 katman dependency chain, default 2dk yetmiyor)
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        var config = ManualConfig.CreateEmpty()
            .AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray())
            .AddLogger(DefaultConfig.Instance.GetLoggers().ToArray())
            .AddExporter(DefaultConfig.Instance.GetExporters().ToArray())
            .AddDiagnoser(DefaultConfig.Instance.GetDiagnosers().ToArray())
            .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
            .AddValidator(DefaultConfig.Instance.GetValidators().ToArray())
            .WithBuildTimeout(TimeSpan.FromMinutes(10));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}
