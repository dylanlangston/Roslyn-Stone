using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class SessionIsolatedExecutionServiceBenchmarks
{
    private SessionIsolatedExecutionService _service = null!;
    private CompilationService _compilationService = null!;
    private SecurityConfiguration _securityConfig = null!;
    private NuGetService _nugetService = null!;

    [GlobalSetup]
    public void Setup()
    {
        _compilationService = new CompilationService();
        _securityConfig = SecurityConfiguration.CreateDevelopmentDefaults();
        _service = new SessionIsolatedExecutionService(_compilationService, _securityConfig);
        _nugetService = new NuGetService();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _nugetService?.Dispose();
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteSimpleExpression()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(contextId, "2 + 2");
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteVariableAssignment()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            int x = 10;
            int y = 20;
            return x + y;
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteLinqQuery()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using System.Linq;
            var numbers = new[] { 1, 2, 3, 4, 5 };
            return numbers.Where(n => n > 2).Sum();
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteComplexExpression()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            
            var people = new List<(string Name, int Age)>
            {
                (""Alice"", 30),
                (""Bob"", 25),
                (""Charlie"", 35)
            };
            
            return people.Where(p => p.Age > 25)
                        .Select(p => p.Name)
                        .OrderBy(n => n)
                        .ToList();
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteWithStringManipulation()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using System;
            var text = ""Hello, World!"";
            return text.ToUpper().Replace(""WORLD"", ""BENCHMARK"");
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteAsyncCode()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using System.Threading.Tasks;
            await Task.Delay(10);
            return ""Completed"";
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteWithConsoleOutput()
    {
        var contextId = Guid.NewGuid().ToString();
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using System;
            Console.WriteLine(""Hello from benchmark"");
            Console.WriteLine(""Line 2"");
            return 42;
            "
        );
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteWithNuGetPackage()
    {
        var contextId = Guid.NewGuid().ToString();

        // Download package first (not timed)
        var assemblyPaths = await _nugetService.DownloadPackageAsync("Newtonsoft.Json", "13.0.1");
        var references = assemblyPaths
            .Where(File.Exists)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

        // Time the execution with the package
        return await _service.ExecuteInContextAsync(
            contextId,
            @"
            using Newtonsoft.Json;
            var obj = new { Name = ""Test"", Value = 123 };
            return JsonConvert.SerializeObject(obj);
            ",
            references
        );
    }

    [Benchmark]
    public async Task<bool> ContextCreationAndUnload()
    {
        var contextId = Guid.NewGuid().ToString();

        // Execute to create context
        await _service.ExecuteInContextAsync(contextId, "return 42;");

        // Unload context
        return await _service.UnloadContextAsync(contextId);
    }
}
