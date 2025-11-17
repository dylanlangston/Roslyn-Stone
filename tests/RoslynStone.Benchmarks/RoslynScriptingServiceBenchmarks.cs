using BenchmarkDotNet.Attributes;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class RoslynScriptingServiceBenchmarks
{
    private RoslynScriptingService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new RoslynScriptingService();
    }

    [Benchmark]
    public async Task<object?> ExecuteSimpleExpression()
    {
        var result = await _service.ExecuteAsync("2 + 2");
        return result.ReturnValue;
    }

    [Benchmark]
    public async Task<object?> ExecuteVariableAssignment()
    {
        var result = await _service.ExecuteAsync("var x = 10; x * 2");
        return result.ReturnValue;
    }

    [Benchmark]
    public async Task<object?> ExecuteLinqQuery()
    {
        var result = await _service.ExecuteAsync(
            "Enumerable.Range(1, 100).Where(x => x % 2 == 0).Sum()"
        );
        return result.ReturnValue;
    }

    [Benchmark]
    public async Task<object?> ExecuteComplexExpression()
    {
        var result = await _service.ExecuteAsync(
            @"
            var list = new List<int> { 1, 2, 3, 4, 5 };
            list.Select(x => x * x).ToList()
        "
        );
        return result.ReturnValue;
    }

    [Benchmark]
    public async Task<object?> ExecuteWithStringManipulation()
    {
        var result = await _service.ExecuteAsync(
            @"
            var text = ""Hello, World!"";
            text.ToUpper().Replace(""WORLD"", ""BENCHMARKS"")
        "
        );
        return result.ReturnValue;
    }
}
