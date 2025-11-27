using BenchmarkDotNet.Attributes;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class DocumentationServiceBenchmarks
{
    private DocumentationService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new DocumentationService();
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_Console()
    {
        return await _service.GetDocumentationAsync("Console");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_String()
    {
        return await _service.GetDocumentationAsync("String");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_List()
    {
        return await _service.GetDocumentationAsync("List");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_Task()
    {
        return await _service.GetDocumentationAsync("Task");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_Linq()
    {
        return await _service.GetDocumentationAsync("Enumerable");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_WithMember()
    {
        return await _service.GetDocumentationAsync("Console.WriteLine");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_GenericType()
    {
        return await _service.GetDocumentationAsync("Dictionary<TKey, TValue>");
    }

    [Benchmark]
    public async Task<DocumentationInfo?> GetDocumentation_Nonexistent()
    {
        return await _service.GetDocumentationAsync("NonExistentTypeForBenchmark");
    }
}
