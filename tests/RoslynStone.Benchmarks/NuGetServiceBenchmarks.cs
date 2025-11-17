using BenchmarkDotNet.Attributes;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class NuGetServiceBenchmarks
{
    private NuGetService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new NuGetService();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _service.Dispose();
    }

    [Benchmark]
    public async Task<int> SearchPackages()
    {
        var result = await _service.SearchPackagesAsync("Newtonsoft.Json", 0, 10);
        return result.Packages.Count;
    }

    [Benchmark]
    public async Task<int> GetPackageVersions()
    {
        var versions = await _service.GetPackageVersionsAsync("Newtonsoft.Json");
        return versions.Count;
    }

    [Benchmark]
    public async Task<string?> GetPackageReadme()
    {
        var readme = await _service.GetPackageReadmeAsync("Newtonsoft.Json");
        return readme;
    }
}
