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
- Use `GetDocumentation` to look up xUnit attributes and .NET testing APIs
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
- **Line Coverage**: > 80%
- **Branch Coverage**: > 70%
- **Critical Paths**: 100%

Focus on testing:
- Public API surface
- Error handling paths
- Edge cases and boundaries
- Security-sensitive code
- Complex business logic

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
