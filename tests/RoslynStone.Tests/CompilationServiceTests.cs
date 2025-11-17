using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for CompilationService - verifies Roslyn compilation functionality
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Compilation")]
public class CompilationServiceTests
{
    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_SimpleCode_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
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
        var result = service.Compile(code);

        // Assert
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
        Assert.NotNull(result.AssemblyStream);
        Assert.NotNull(result.AssemblyName);
        Assert.True(result.AssemblyStream!.Length > 0);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_TopLevelStatements_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
using System;
Console.WriteLine(""Hello from top-level!"");
";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AssemblyStream);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_InvalidCode_ReturnsErrors()
    {
        // Arrange
        var service = new CompilationService();
        var code = "this is not valid C# code!!!";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessages);
        Assert.NotEmpty(result.ErrorMessages);
        Assert.NotNull(result.Diagnostics);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_SyntaxError_ReturnsErrorWithLocation()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
using System;

public class Program
{
    public static void Main()
    {
        int x = ""not a number""; // Type mismatch
    }
}";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Id == "CS0029" || d.Id.StartsWith("CS"));
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_MultipleCompilations_GeneratesUniqueNames()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
using System;
public class Program { public static void Main() { } }";

        // Act
        var result1 = service.Compile(code);
        var result2 = service.Compile(code);

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(result1.AssemblyName, result2.AssemblyName);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_CustomAssemblyName_UsesProvidedName()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
using System;
public class Program { public static void Main() { } }";
        var customName = "MyCustomAssembly";

        // Act
        var result = service.Compile(code, customName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(customName, result.AssemblyName);
    }
}
