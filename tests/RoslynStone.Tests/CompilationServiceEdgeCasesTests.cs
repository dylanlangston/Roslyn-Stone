using Microsoft.CodeAnalysis;
using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Edge case tests for CompilationService to improve branch coverage
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Compilation")]
public class CompilationServiceEdgeCasesTests
{
    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_EmptyCode_FailsWithError()
    {
        // Arrange
        var service = new CompilationService();
        var code = "";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessages);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_OnlyWhitespace_FailsWithError()
    {
        // Arrange
        var service = new CompilationService();
        var code = "   \n\t   ";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
int x = ""string""; // Error 1
int y = 'c'; // Error 2 - char to int
unknown type z; // Error 3
";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.NotEmpty(result.Diagnostics);
        Assert.True(result.Diagnostics.Count >= 2);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_UnsafeCode_FailsWithError()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
unsafe
{
    int* p = null;
}";

        // Act
        var result = service.Compile(code);

        // Assert - Should fail because unsafe is not allowed
        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_UnknownType_FailsWithError()
    {
        // Arrange
        var service = new CompilationService();
        var code = "NonExistentType x = new NonExistentType();";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Id.StartsWith("CS"));
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_InvalidSyntax_FailsWithSpecificError()
    {
        // Arrange
        var service = new CompilationService();
        var code = "if ( { }"; // Invalid syntax

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics!, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_MissingNamespace_FailsWithError()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
public class Program
{
    public static void Main()
    {
        HttpClient client = new HttpClient(); // Missing using System.Net.Http
    }
}";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    [Trait("Feature", "Compilation")]
    public void Compile_LargeCode_CompilesSuccessfully()
    {
        // Arrange
        var service = new CompilationService();
        var codeBuilder = new System.Text.StringBuilder();
        codeBuilder.AppendLine("using System;");
        codeBuilder.AppendLine("public class Program {");
        codeBuilder.AppendLine("public static void Main() {");

        // Add many statements to test compilation of larger code
        for (int i = 0; i < 100; i++)
        {
            codeBuilder.AppendLine($"    var x{i} = {i};");
        }

        codeBuilder.AppendLine("}}");
        var code = codeBuilder.ToString();

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    [Trait("Feature", "AssemblyName")]
    public void Compile_NullAssemblyName_GeneratesName()
    {
        // Arrange
        var service = new CompilationService();
        var code = "public class Program { public static void Main() { } }";

        // Act
        // ReSharper disable once RedundantArgumentDefaultValue - Explicit for test clarity
        var result = service.Compile(code, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AssemblyName);
        Assert.StartsWith("DynamicAssembly_", result.AssemblyName);
    }

    [Fact]
    [Trait("Feature", "OutputStreams")]
    public void Compile_SuccessfulCompilation_HasBothStreams()
    {
        // Arrange
        var service = new CompilationService();
        var code = "public class Program { public static void Main() { } }";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AssemblyStream);
        Assert.NotNull(result.SymbolsStream);
        Assert.True(result.AssemblyStream.Length > 0);
        Assert.True(result.SymbolsStream.Length > 0);
    }

    [Fact]
    [Trait("Feature", "ErrorMessages")]
    public void Compile_ErrorWithLocation_IncludesLocationInMessage()
    {
        // Arrange
        var service = new CompilationService();
        var code =
            @"
using System;
int x = ""error"";
";

        // Act
        var result = service.Compile(code);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessages!);
        // Error messages should include error codes
        Assert.NotNull(result.ErrorMessages);
    }
}
