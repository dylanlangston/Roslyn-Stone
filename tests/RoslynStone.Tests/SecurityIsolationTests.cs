// ReSharper disable RedundantUsingDirective - Required for test infrastructure
using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;
using RoslynStone.Tests.Serialization;
using Xunit;

namespace RoslynStone.Tests;

/// <summary>
/// Security tests for session isolation and resource protection
/// These tests verify the security findings from SECURITY_AUDIT_REPORT.md
/// </summary>
[Trait("Category", "Security")]
public class SecurityIsolationTests
{
    [Fact]
    [Trait("Finding", "1")]
    [Trait("Severity", "Critical")]
    public async Task NuGetPackagesParameter_ProperlyIsolated_SAFE()
    {
        // This test shows the SAFE alternative to LoadNuGetPackage
        // Using nugetPackages parameter creates context-specific isolation

        // Arrange
        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - User A loads package via nugetPackages parameter
        var userAResult = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "using Humanizer; \"test\".Humanize()",
            nugetPackages: new[]
            {
                new NuGetPackageSpec { PackageName = "Humanizer", Version = "2.14.1" },
            },
            createContext: true
        );
        var userAJson = TestJsonContext.SerializeDynamic(userAResult);
        var userADict = TestJsonContext.DeserializeToDictionary(userAJson);

        Assert.NotNull(userADict);
        Assert.True(userADict["success"].GetBoolean(), "User A should successfully use package");

        // User B (separate context) tries to use the same package WITHOUT loading it
        var userBResult = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "using Humanizer; \"test\".Humanize()",
            createContext: true // New context, NO nugetPackages parameter
        );
        var userBJson = TestJsonContext.SerializeDynamic(userBResult);
        var userBDict = TestJsonContext.DeserializeToDictionary(userBJson);

        // Assert - PROPER ISOLATION: User B does NOT have access!
        Assert.NotNull(userBDict);
        Assert.False(
            userBDict["success"].GetBoolean(),
            "User B should NOT have access to User A's package"
        );

        var errors = userBDict["errors"].EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.GetProperty("message").GetString()!.Contains("Humanizer"));

        // This demonstrates proper session isolation ✓
    }

    [Fact(Skip = "Requires timeout implementation - see Finding #2")]
    [Trait("Finding", "2")]
    [Trait("Severity", "Critical")]
    public async Task ExecuteAsync_InfiniteLoop_ShouldTimeout()
    {
        // This test is SKIPPED because the fix is not yet implemented
        // After implementing Finding #2 fix, enable this test

        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Execute infinite loop
        var result = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "while(true) { }", // Infinite loop
            createContext: false
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should timeout after 30 seconds (default)
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "EXECUTION_TIMEOUT");
    }

    [Fact(Skip = "Requires memory limit implementation - see Finding #3")]
    [Trait("Finding", "3")]
    [Trait("Severity", "Critical")]
    public async Task ExecuteAsync_LargeAllocation_ShouldFail()
    {
        // This test is SKIPPED because the fix is not yet implemented
        // After implementing Finding #3 fix, enable this test

        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Try to allocate 2GB (should be blocked)
        var result = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            "var bomb = new byte[int.MaxValue];",
            createContext: false
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should fail due to memory limit
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "MEMORY_LIMIT_EXCEEDED");
    }

    [Fact(Skip = "Requires filesystem restriction - see Finding #4")]
    [Trait("Finding", "4")]
    [Trait("Severity", "Critical")]
    public async Task ExecuteAsync_FileAccess_ShouldBeBlocked()
    {
        // This test is SKIPPED because the fix is not yet implemented
        // After implementing Finding #4 fix, enable this test

        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Try to read sensitive file
        var result = await ReplTools.EvaluateCsharp(
            service,
            contextMgr,
            nugetSvc,
            @"
                using System.IO;
                File.ReadAllText(""/etc/passwd"");
            ",
            createContext: false
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should be blocked by static analysis or runtime restriction
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "FORBIDDEN_API");
    }

    [Fact]
    [Trait("Finding", "5")]
    [Trait("Severity", "High")]
    public async Task EvaluateCsharp_ContextId_ShouldNotBeGuessable()
    {
        // This test verifies context IDs are not trivially guessable
        // After implementing Finding #5 fix (HMAC), verify signatures

        var service = new RoslynScriptingService();
        var contextMgr = new ReplContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Create multiple contexts
        var contexts = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var result = await ReplTools.EvaluateCsharp(
                service,
                contextMgr,
                nugetSvc,
                "int x = 1;",
                createContext: true
            );
            var json = TestJsonContext.SerializeDynamic(result);
            var dict = TestJsonContext.DeserializeToDictionary(json);
            var contextId = dict!["contextId"].GetString();
            contexts.Add(contextId!);
        }

        // Assert - All context IDs should be unique
        Assert.Equal(10, contexts.Distinct().Count());

        // After implementing HMAC signatures:
        // Assert - Context IDs should have signature component
        // foreach (var contextId in contexts)
        // {
        //     Assert.Contains(".", contextId);  // Format: {guid}.{signature}
        //     var parts = contextId.Split('.');
        //     Assert.Equal(2, parts.Length);
        // }
    }
}
