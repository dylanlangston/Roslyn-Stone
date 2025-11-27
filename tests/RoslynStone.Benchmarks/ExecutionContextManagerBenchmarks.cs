using BenchmarkDotNet.Attributes;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Benchmarks;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ExecutionContextManagerBenchmarks
{
    private IExecutionContextManager _contextManager = null!;
    private SessionIsolatedExecutionService _executionService = null!;
    private string _existingContextId = null!;

    [GlobalSetup]
    public void Setup()
    {
        var compilationService = new CompilationService();
        var securityConfig = SecurityConfiguration.CreateDevelopmentDefaults();
        _executionService = new SessionIsolatedExecutionService(compilationService, securityConfig);
        _contextManager = new ExecutionContextManager(
            contextTimeout: TimeSpan.FromMinutes(5),
            securityConfig: securityConfig
        );

        // Create a context for existence checks
        _existingContextId = _contextManager.CreateContext();
    }

    [Benchmark]
    public string CreateContext()
    {
        return _contextManager.CreateContext();
    }

    [Benchmark]
    public bool ContextExists_Existing()
    {
        return _contextManager.ContextExists(_existingContextId);
    }

    [Benchmark]
    public bool ContextExists_Nonexistent()
    {
        return _contextManager.ContextExists("nonexistent-context-id");
    }

    [Benchmark]
    public async Task CreateExecuteAndUnload()
    {
        // Complete workflow: create context, execute code, unload from execution service
        var contextId = _contextManager.CreateContext();

        if (_contextManager.ContextExists(contextId))
        {
            await _executionService.ExecuteInContextAsync(contextId, "return 42;");
            await _executionService.UnloadContextAsync(contextId);
        }
    }
}
