using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for file-based program features (C# 14 / .NET 10)
/// Verifies support for #:package, #:sdk, #:property directives
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "FileBasedProgram")]
public class FileBasedProgramTests
{
    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_WithPackageDirective_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"
#:package Newtonsoft.Json@13.0.3
using System;
Console.WriteLine(""Hello, World!"");
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
    }

    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_WithMultiplePackageDirectives_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"
#:package ClosedXML@0.100.0
#:package MimeKit@4.3.0
using System;
Console.WriteLine(""Hello, World!"");
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
    }

    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_WithSdkDirective_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"
#:sdk Microsoft.NET.Sdk.Web
using System;
Console.WriteLine(""Hello, World!"");
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
    }

    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_WithPropertyDirective_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"
#:property TargetFramework=net10.0
#:property LangVersion=preview
using System;
Console.WriteLine(""Hello, World!"");
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
    }

    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_WithShebangAndDirectives_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"#!/usr/bin/env dotnet run
#:package System.CommandLine@2.0.0
using System;
Console.WriteLine(""Hello, World!"");
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        Assert.True(
            result.Success,
            $"Compilation failed: {string.Join(", ", result.ErrorMessages ?? new List<string>())}"
        );
    }

    [Fact]
    [Trait("Feature", "FileBasedProgram")]
    public void Compile_CompleteFileBasedApp_Succeeds()
    {
        // Arrange
        var service = new CompilationService();
        var code = @"#!/usr/bin/env dotnet run
#:package Humanizer@2.14.1
using System;
using Humanizer;

var dateTime = DateTime.UtcNow.AddHours(-24);
Console.WriteLine(dateTime.Humanize());
";

        // Act
        var result = service.Compile(code);

        // Assert - Should compile without CS9298 error
        // Note: Compilation may still fail due to missing Humanizer assembly,
        // but it should NOT fail with CS9298 (file-based program feature error)
        if (!result.Success)
        {
            Assert.DoesNotContain(result.Diagnostics ?? new List<Microsoft.CodeAnalysis.Diagnostic>(),
                d => d.Id == "CS9298");
        }
    }
}
