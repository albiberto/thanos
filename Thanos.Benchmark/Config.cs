using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace Thanos.Benchmark;

public class Config : ManualConfig
{
    public Config()
    {
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}