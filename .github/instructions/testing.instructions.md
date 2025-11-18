---
scope:
  patterns:
    - "**/test/**"
    - "**/tests/**"
    - "**/*Test.cs"
    - "**/*Tests.cs"
    - "**/*.test.cs"
    - "**/*.tests.cs"
---

# Testing Instructions

## Using MCP Tools for Test Development

When writing tests, use the roslyn-stone MCP tools to validate test code:
- Use `ValidateCsharp` to check test code syntax
- Use `EvaluateCsharp` to verify test assertions and expected behavior
- Access `doc://{symbolName}` resources to look up xUnit attributes and .NET testing APIs
- This ensures test code is correct before running the full test suite

## Testing Framework

Use xUnit as the primary testing framework for consistency with .NET conventions.

### Test Structure
```csharp
public class ComponentTests
{
    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange
        var component = new Component();
        var input = "test data";
        
        // Act
        var result = component.Method(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("input1", "expected1")]
    [InlineData("input2", "expected2")]
    public void MethodName_WithVariousInputs_ReturnsExpectedResults(
        string input, string expected)
    {
        // Arrange & Act
        var result = Component.Method(input);
        
        // Assert
        Assert.Equal(expected, result);
    }
}
```

## Test Categories

### Unit Tests
- Test individual components in isolation
- Mock external dependencies
- Fast execution (< 100ms per test)
- Deterministic and repeatable

### Integration Tests
```csharp
[Collection("Integration")]
public class McpServerIntegrationTests
{
    [Fact]
    public async Task McpServer_ExecutesTool_ReturnsExpectedResult()
    {
        // Arrange
        using var server = CreateTestServer();
        await server.StartAsync();
        
        // Act
        var result = await server.ExecuteToolAsync("tool_name", parameters);
        
        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        
        // Cleanup
        await server.StopAsync();
    }
}
```

### End-to-End Tests
- Test complete workflows
- Use minimal mocking
- Test realistic scenarios
- May be slower but provide high confidence

## Mocking

Use Moq for creating test doubles:
```csharp
[Fact]
public async Task Service_WithMockedDependency_BehavesCorrectly()
{
    // Arrange
    var mockRepo = new Mock<IRepository>();
    mockRepo.Setup(r => r.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(new Entity());
    
    var service = new Service(mockRepo.Object);
    
    // Act
    var result = await service.ProcessAsync("test");
    
    // Assert
    mockRepo.Verify(r => r.GetAsync("test"), Times.Once);
    Assert.NotNull(result);
}
```

## Test Data

### Test Fixtures
```csharp
public class TestFixture : IDisposable
{
    public TestServer Server { get; }
    
    public TestFixture()
    {
        Server = CreateTestServer();
    }
    
    public void Dispose()
    {
        Server?.Dispose();
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<TestFixture>
{
}
```

### Test Data Builders
```csharp
public class EntityBuilder
{
    private string _name = "default";
    private int _value = 0;
    
    public EntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public EntityBuilder WithValue(int value)
    {
        _value = value;
        return this;
    }
    
    public Entity Build() => new Entity { Name = _name, Value = _value };
}
```

## REPL Testing

Test REPL functionality thoroughly:
```csharp
[Fact]
public async Task Repl_EvaluatesSimpleExpression_ReturnsResult()
{
    // Arrange
    var repl = new CSharpRepl();
    
    // Act
    var result = await repl.EvaluateAsync("1 + 1");
    
    // Assert
    Assert.True(result.Success);
    Assert.Equal(2, result.Value);
    Assert.Equal("System.Int32", result.Type);
}

[Fact]
public async Task Repl_WithSyntaxError_ReturnsActionableError()
{
    // Arrange
    var repl = new CSharpRepl();
    
    // Act
    var result = await repl.EvaluateAsync("var x = ");
    
    // Assert
    Assert.False(result.Success);
    Assert.NotNull(result.Error);
    Assert.Contains("syntax error", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    Assert.NotEmpty(result.Error.Diagnostics);
}
```

## MCP Testing

Test MCP server operations:
```csharp
[Fact]
public async Task McpServer_DiscoverTools_ReturnsAllTools()
{
    // Arrange
    var server = CreateMcpServer();
    
    // Act
    var tools = await server.DiscoverToolsAsync();
    
    // Assert
    Assert.NotEmpty(tools);
    Assert.All(tools, tool =>
    {
        Assert.NotNull(tool.Name);
        Assert.NotNull(tool.Description);
        Assert.NotNull(tool.Schema);
    });
}
```

## Error Testing

Always test error conditions:
```csharp
[Fact]
public async Task Method_WithInvalidInput_ThrowsArgumentException()
{
    // Arrange
    var component = new Component();
    
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        async () => await component.MethodAsync(null)
    );
}

[Fact]
public async Task Method_WithInvalidInput_ReturnsErrorResult()
{
    // Arrange
    var component = new Component();
    
    // Act
    var result = await component.TryMethodAsync(null);
    
    // Assert
    Assert.False(result.Success);
    Assert.NotNull(result.Error);
    Assert.Contains("invalid input", result.Error.Message);
}
```

## Async Testing

Properly test async operations:
```csharp
[Fact]
public async Task AsyncMethod_CompletesSuccessfully()
{
    // Arrange
    var service = new Service();
    
    // Act
    var result = await service.ExecuteAsync();
    
    // Assert
    Assert.NotNull(result);
}

[Fact]
public async Task AsyncMethod_WithCancellation_ThrowsOperationCanceledException()
{
    // Arrange
    var service = new Service();
    var cts = new CancellationTokenSource();
    cts.Cancel();
    
    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        async () => await service.ExecuteAsync(cts.Token)
    );
}
```

## Test Naming

Use descriptive test names:
- `Method_Scenario_ExpectedBehavior`
- `Given_Precondition_When_Action_Then_Outcome`
- Be specific and clear about what is being tested

## Assertions

Use appropriate assertions:
- `Assert.Equal()` for value equality
- `Assert.Same()` for reference equality
- `Assert.True()/False()` for boolean conditions
- `Assert.NotNull()` for existence checks
- `Assert.Throws<>()` for exception testing
- `Assert.Collection()` for collection testing

## Test Coverage

Aim for:
- **Line Coverage**: > 80% (currently 86.67%)
- **Branch Coverage**: > 75% (currently 62.98%)
- **Critical Paths**: 100%

Focus on testing:
- Public API surface
- Error handling paths
- Edge cases and boundaries
- Security-sensitive code
- Complex business logic

### Running Coverage Reports

```bash
# Run tests with coverage
dotnet cake --target=Test-Coverage

# Generate HTML coverage report
dotnet cake --target=Test-Coverage-Report
# Opens at ./artifacts/coverage-report/index.html
```

The coverage task:
- Runs all tests with code coverage collection
- Validates line and branch coverage thresholds
- Generates Cobertura XML reports
- Displays coverage percentages in CI output
- Warns if coverage falls below thresholds (80% line, 75% branch)

### Coverage Best Practices

- Write tests that exercise different code paths (branches)
- Test both success and error scenarios
- Test edge cases and boundary conditions
- Use `DefaultIfEmpty()` before `Average()` to handle empty sequences
- Proper resource disposal with `using` statements improves coverage accuracy

## Performance Testing

For performance-critical code:
```csharp
[Fact]
public async Task Method_Completes_WithinTimeLimit()
{
    // Arrange
    var component = new Component();
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    await component.ExpensiveOperationAsync();
    stopwatch.Stop();
    
    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
        $"Operation took {stopwatch.ElapsedMilliseconds}ms");
}
```

## Best Practices

1. **Independence**: Tests should not depend on each other
2. **Repeatability**: Tests should produce the same result every time
3. **Clarity**: Test code should be readable and maintainable
4. **Speed**: Keep tests fast to encourage frequent execution
5. **Isolation**: Use mocks to isolate the system under test
6. **AAA Pattern**: Arrange, Act, Assert structure
7. **One Assertion**: Focus each test on one logical assertion
8. **Test Names**: Use descriptive names that explain the test
9. **Resource Disposal**: Always use `using` for IDisposable (CancellationTokenSource, HttpClient, etc.)
10. **Avoid Code Smells**: No `Assert.True(true)` or tautology assertions

## Benchmarking (BenchmarkDotNet)

The `RoslynStone.Benchmarks` project tracks performance of critical operations.

### Available Benchmarks

- **RoslynScriptingServiceBenchmarks**: REPL execution performance (5 scenarios)
  - Simple expressions, variable assignments, LINQ queries, complex operations, string manipulation
- **CompilationServiceBenchmarks**: Code compilation performance (4 scenarios)
  - Simple class compilation, complex code, error handling, multiple classes
- **NuGetServiceBenchmarks**: Package operations performance (3 scenarios)
  - Package search, version lookup, README retrieval

### Running Benchmarks

```bash
# Run all benchmarks in Release mode
dotnet cake --target=Benchmark

# Run specific benchmark class
dotnet run --project tests/RoslynStone.Benchmarks --configuration Release -- --filter *RoslynScriptingService*

# Run with custom BenchmarkDotNet options
dotnet run --project tests/RoslynStone.Benchmarks --configuration Release -- --job short
```

### Benchmark Best Practices

- Always run in **Release** configuration
- Close other applications to minimize interference
- Run multiple iterations for statistical significance
- Use `[MemoryDiagnoser]` to track allocations
- Add `[GlobalSetup]` and `[GlobalCleanup]` for resource management
- Results saved to `./artifacts/benchmarks/`

### Adding New Benchmarks

```csharp
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class MyServiceBenchmarks
{
    private MyService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        _service = new MyService();
    }

    [Benchmark]
    public async Task<ResultType> BenchmarkOperation()
    {
        return await _service.OperationAsync();
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _service?.Dispose();
    }
}
```

## Load Testing

The `RoslynStone.LoadTests` project validates HTTP MCP server scalability.

### Load Test Configuration

- **Default concurrency**: 300 concurrent requests per round
- **Default rounds**: 10 rounds per scenario
- **Test scenarios**: 4 (expressions, LINQ, variable assignments, NuGet search)
- **Total requests**: 12,000 (300 × 10 × 4)
- **Metrics**: Throughput, latency, success rate, response times

### Running Load Tests

```bash
# 1. Start the server in HTTP mode
cd src/RoslynStone.Api
MCP_TRANSPORT=http dotnet run

# 2. In another terminal, run load tests
cd /path/to/repo
dotnet cake --target=Load-Test

# Or with custom configuration
dotnet run --project tests/RoslynStone.LoadTests -- http://localhost:7071 300 10
# Arguments: [baseUrl] [concurrency] [rounds]
```

### Expected Performance

A healthy server should achieve:
- ✅ Success rate > 99%
- ✅ Average response time < 100ms for simple operations
- ✅ Throughput > 1000 requests/second

### Load Test Metrics

The tool reports for each scenario:
- **Average Round Time**: Time to complete all concurrent requests
- **Average Response Time**: Per-request response time
- **Success Rate**: Percentage of successful requests
- **Throughput**: Requests per second
- **Total Success/Failures**: Request counts

### Adding Load Test Scenarios

```csharp
private static string CreateNewScenarioRequest()
{
    var request = new
    {
        jsonrpc = "2.0",
        method = "tools/call",
        @params = new
        {
            name = "ToolName",
            arguments = new { param = "value" }
        },
        id = 1
    };
    return JsonSerializer.Serialize(request);
}
```

## CI Integration

All test infrastructure is integrated with CI:

```bash
# Full CI pipeline (includes coverage validation)
dotnet cake --target=CI

# Individual tasks
dotnet cake --target=Format-Check    # CSharpier formatting
dotnet cake --target=Inspect         # ReSharper analysis
dotnet cake --target=Build           # Build solution
dotnet cake --target=Test-Coverage   # Tests with coverage
```

### CI Artifacts

- Test results (`.trx` files)
- Coverage reports (Cobertura XML)
- ReSharper inspection reports
- Build logs

## Test Organization

```
tests/
├── RoslynStone.Tests/          # Unit & integration tests (xUnit)
│   ├── *ServiceTests.cs        # Service-level unit tests
│   ├── *IntegrationTests.cs    # Integration test suites
│   ├── DiagnosticHelpersTests.cs
│   └── CompilationServiceEdgeCasesTests.cs
├── RoslynStone.Benchmarks/     # Performance benchmarks
│   ├── *ServiceBenchmarks.cs
│   ├── Program.cs
│   └── README.md
└── RoslynStone.LoadTests/      # Load & concurrency tests
    ├── Program.cs
    └── README.md
```
