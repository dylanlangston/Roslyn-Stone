using RoslynStone.Infrastructure.Services;

namespace RoslynStone.Tests;

/// <summary>
/// Tests for file-based program features in scripting/REPL context
/// Verifies that #:package, #:sdk, #:property directives are properly handled in REPL
/// </summary>
[Trait("Category", "Unit")]
[Trait("Component", "REPL")]
[Trait("Feature", "FileBasedProgram")]
public class FileBasedProgramScriptingTests
{
    [Fact]
    public async Task ExecuteAsync_WithPackageDirective_StripsDirectiveAndExecutes()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"#:package Newtonsoft.Json@13.0.3
using System;
Console.WriteLine(""Hello, World!"");
""success""";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success, $"Execution failed: {string.Join(", ", result.Errors.Select(e => e.Message))}");
        Assert.Equal("success", result.ReturnValue);
        Assert.Contains("Hello, World!", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleDirectives_StripsAllDirectivesAndExecutes()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"#:package ClosedXML@0.100.0
#:package MimeKit@4.3.0
#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
using System;
Console.WriteLine(""Test"");
42";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.ReturnValue);
        Assert.Contains("Test", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_WithShebangAndDirectives_StripsAllAndExecutes()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"#!/usr/bin/env dotnet run
#:package System.CommandLine@2.0.0
using System;
Console.WriteLine(""Shebang test"");
100";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(100, result.ReturnValue);
        Assert.Contains("Shebang test", result.Output);
    }

    [Fact]
    public async Task ExecuteWithStateAsync_WithPackageDirective_StripsAndExecutes()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"#:package Humanizer@2.14.1
var x = 10;
x * 2";

        // Act
        var result = await service.ExecuteWithStateAsync(code, null);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(20, result.ReturnValue);
        Assert.NotNull(result.ScriptState);
    }

    [Fact]
    public async Task ExecuteAsync_WithDirectivesInMiddle_OnlyStripsDirectiveLines()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"var a = 5;
#:package Test@1.0.0
var b = 10;
a + b";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15, result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoDirectives_ExecutesNormally()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"using System;
var result = ""no directives"";
result";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("no directives", result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithCommentedDirective_DoesNotStripComment()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"// This is a comment: #:package Test@1.0.0
var x = ""comment test"";
x";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("comment test", result.ReturnValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithDirectiveInString_DoesNotStripString()
    {
        // Arrange
        var service = new RoslynScriptingService();
        var code = @"var text = ""This contains #:package in string"";
text";

        // Act
        var result = await service.ExecuteAsync(code);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("This contains #:package in string", result.ReturnValue);
    }
}
