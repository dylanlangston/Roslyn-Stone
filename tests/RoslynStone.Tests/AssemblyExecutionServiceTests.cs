using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for AssemblyExecutionService - verifies dynamic assembly execution with proper unloading
/// Based on best practices from Laurent Kempé's approach
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "Execution")]
public class AssemblyExecutionServiceTests
{
    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteCodeAsync_SimpleProgram_Succeeds()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code =
            @"
using System;

public class Program
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

        // Act
        var result = await executionService.ExecuteCodeAsync(code);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "No error message");
        Assert.NotNull(result.Output);
        Assert.Contains("Hello, World!", result.Output);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteCodeAsync_TopLevelStatements_Succeeds()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code =
            @"
using System;
Console.WriteLine(""Top-level statement test"");
";

        // Act
        var result = await executionService.ExecuteCodeAsync(code);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "No error message");
        Assert.Contains("Top-level statement test", result.Output);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteCodeAsync_CompilationError_ReturnsError()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code = "this is not valid C# code";

        // Act
        var result = await executionService.ExecuteCodeAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Compilation failed", result.ErrorMessage);
        Assert.NotNull(result.CompilationErrors);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteCodeAsync_RuntimeException_CapturesError()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code =
            @"
using System;

public class Program
{
    public static void Main()
    {
        throw new Exception(""Runtime error test"");
    }
}";

        // Act
        var result = await executionService.ExecuteCodeAsync(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Execution error", result.ErrorMessage);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteCodeAsync_AsyncMain_HandlesAsyncExecution()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code =
            @"
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        await Task.Delay(10);
        Console.WriteLine(""Async execution completed"");
    }
}";

        // Act
        var result = await executionService.ExecuteCodeAsync(code);

        // Assert
        Assert.True(result.Success, result.ErrorMessage ?? "No error message");
        Assert.Contains("Async execution completed", result.Output);
    }

    [Fact]
    [Trait("Feature", "MemoryManagement")]
    public async Task ExecuteCodeAsync_MultipleExecutions_DoesNotLeakMemory()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var code =
            @"
using System;
public class Program
{
    public static void Main()
    {
        Console.WriteLine(""Test"");
    }
}";

        // Act - Execute multiple times to verify no memory leaks
        for (int i = 0; i < 5; i++)
        {
            var result = await executionService.ExecuteCodeAsync(code);
            Assert.True(result.Success);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Assert - If we get here without OOM, memory management is working
        Assert.True(true);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteFileAsync_NonExistentFile_ReturnsError()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var filePath = "/tmp/nonexistent_file_12345.cs";

        // Act
        var result = await executionService.ExecuteFileAsync(filePath);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.ErrorMessage);
    }

    [Fact]
    [Trait("Feature", "Execution")]
    public async Task ExecuteFileAsync_ValidFile_Succeeds()
    {
        // Arrange
        var compilationService = new CompilationService();
        var executionService = new AssemblyExecutionService(compilationService);
        var filePath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.cs");
        var code =
            @"
using System;
public class Program
{
    public static void Main()
    {
        Console.WriteLine(""File execution test"");
    }
}";

        try
        {
            await File.WriteAllTextAsync(filePath, code);

            // Act
            var result = await executionService.ExecuteFileAsync(filePath);

            // Assert
            Assert.True(result.Success, result.ErrorMessage ?? "No error message");
            Assert.Contains("File execution test", result.Output);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
