using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynStone.Infrastructure.Functional;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for DiagnosticHelpers functional helpers
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "Functional")]
public class DiagnosticHelpersTests
{
    [Fact]
    [Trait("Feature", "Conversion")]
    public void ToCompilationError_ValidDiagnostic_ConvertsCorrectly()
    {
        // Arrange
        var code = "int x = \"string\";"; // Type mismatch
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation.GetDiagnostics();
        var errorDiagnostic = diagnostics.First(d => d.Severity == DiagnosticSeverity.Error);

        // Act
        var error = errorDiagnostic.ToCompilationError();

        // Assert
        Assert.NotNull(error);
        Assert.NotEmpty(error.Code);
        Assert.NotEmpty(error.Message);
        Assert.NotEmpty(error.Severity);
        Assert.True(error.Line > 0);
        Assert.True(error.Column > 0);
    }

    [Fact]
    [Trait("Feature", "Filtering")]
    public void ToCompilationErrors_MixedDiagnostics_FiltersErrorsAndWarnings()
    {
        // Arrange
        var code =
            @"
using System;
int x = ""string""; // Error
#pragma warning disable CS0168
int y;  // Warning (unused variable)
#pragma warning restore CS0168
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation.GetDiagnostics();

        // Act
        var errors = diagnostics.ToCompilationErrors();

        // Assert
        Assert.NotEmpty(errors);
        Assert.All(errors, e => Assert.True(e.Severity == "Error" || e.Severity == "Warning"));
    }

    [Fact]
    [Trait("Feature", "Filtering")]
    public void ToCompilationErrors_OnlyInfoDiagnostics_ReturnsEmpty()
    {
        // Arrange - Valid code with no errors or warnings
        var code = "int x = 42;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation
            .GetDiagnostics()
            .Where(d =>
                d.Severity == DiagnosticSeverity.Info || d.Severity == DiagnosticSeverity.Hidden
            );

        // Act
        var errors = diagnostics.ToCompilationErrors();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    [Trait("Feature", "Partitioning")]
    public void PartitionDiagnostics_ErrorsAndWarnings_PartitionsCorrectly()
    {
        // Arrange - Code with both errors and warnings
        var code =
            @"
using System;
int x = ""string""; // Error: CS0029
int y;  // Warning: CS0168 (unused variable)
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation.GetDiagnostics();

        // Act
        var (errors, warnings) = diagnostics.PartitionDiagnostics();

        // Assert
        Assert.NotEmpty(errors);
        Assert.All(errors, e => Assert.Equal("Error", e.Severity));

        // Warnings might be present depending on compilation settings
        Assert.All(warnings, w => Assert.Equal("Warning", w.Severity));
    }

    [Fact]
    [Trait("Feature", "Partitioning")]
    public void PartitionDiagnostics_OnlyErrors_ReturnsEmptyWarnings()
    {
        // Arrange - Code with only errors
        var code =
            @"
int x = ""string""; // Error
int y = ""another string""; // Another error
";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error);

        // Act
        var (errors, warnings) = diagnostics.PartitionDiagnostics();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Empty(warnings);
    }

    [Fact]
    [Trait("Feature", "Partitioning")]
    public void PartitionDiagnostics_OnlyWarnings_ReturnsEmptyErrors()
    {
        // Arrange - Code with only warnings
        var code =
            @"
using System;
int x;  // CS0168: Variable is declared but never used
int y;  // CS0168: Variable is declared but never used
";
        var tree = CSharpSyntaxTree.ParseText(
            code,
            // ReSharper disable once RedundantNameQualifier - Explicit for clarity
            CSharpParseOptions.Default.WithLanguageVersion(
                Microsoft.CodeAnalysis.CSharp.LanguageVersion.Latest
            )
        );
        var compilation = CSharpCompilation
            .Create(
                "test",
                options: new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    warningLevel: 4
                )
            )
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Warning);

        // Act
        var (errors, warnings) = diagnostics.PartitionDiagnostics();

        // Assert
        Assert.Empty(errors);
        Assert.NotEmpty(warnings);
    }

    [Fact]
    [Trait("Feature", "Check")]
    public void HasErrors_DiagnosticsWithErrors_ReturnsTrue()
    {
        // Arrange
        var code = "int x = \"string\";"; // Error
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation.GetDiagnostics();

        // Act
        var hasErrors = diagnostics.HasErrors();

        // Assert
        Assert.True(hasErrors);
    }

    [Fact]
    [Trait("Feature", "Check")]
    public void HasErrors_DiagnosticsWithoutErrors_ReturnsFalse()
    {
        // Arrange
        var code = "int x = 42;"; // Valid code
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation
            .Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = compilation
            .GetDiagnostics()
            .Where(d => d.Severity != DiagnosticSeverity.Error);

        // Act
        var hasErrors = diagnostics.HasErrors();

        // Assert
        Assert.False(hasErrors);
    }

    [Fact]
    [Trait("Feature", "Check")]
    public void HasErrors_EmptyDiagnostics_ReturnsFalse()
    {
        // Arrange
        var diagnostics = Array.Empty<Diagnostic>();

        // Act
        var hasErrors = diagnostics.HasErrors();

        // Assert
        Assert.False(hasErrors);
    }
}
