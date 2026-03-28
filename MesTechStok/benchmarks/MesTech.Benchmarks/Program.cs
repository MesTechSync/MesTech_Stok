using BenchmarkDotNet.Running;

namespace MesTech.Benchmarks;

/// <summary>
/// MesTech Benchmark Runner — BenchmarkDotNet ile mikro-benchmark.
/// Kullanim: dotnet run -c Release --project benchmarks/MesTech.Benchmarks/
/// Tum benchmark'lar: dotnet run -c Release -- --filter *
/// Tek benchmark: dotnet run -c Release -- --filter *HandlerBenchmarks*
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
