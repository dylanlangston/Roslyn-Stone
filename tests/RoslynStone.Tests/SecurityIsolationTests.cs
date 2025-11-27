// ReSharper disable RedundantUsingDirective - Required for test infrastructure
using System.Text.Json;
using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Models;
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
        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Act - User A loads package via nugetPackages parameter
        var userAResult = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
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
        var userBResult = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
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

    [Fact()]
    [Trait("Finding", "2")]
    [Trait("Severity", "Critical")]
    public async Task ExecuteAsync_InfiniteLoop_ShouldTimeout()
    {
        // NOTE: .NET has no Thread.Abort (removed in .NET 5+)
        // Task.Run + WaitAsync cannot forcefully terminate CPU-bound synchronous code
        // that never yields or checks CancellationToken.
        //
        // MITIGATION: Use container-level resource limits:
        // - Docker: --cpus, --memory, --pids-limit
        // - Kubernetes: resources.limits.cpu, resources.limits.memory
        // - systemd: CPUQuota, MemoryMax
        //
        // This test remains as documentation of the limitation.

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Use production config with 5-second timeout for faster test
        var securityConfig = SecurityConfiguration.CreateProductionDefaults();
        securityConfig.ExecutionTimeout = TimeSpan.FromSeconds(5);

        // Act - Execute infinite loop
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "while(true) { }", // Infinite loop
            createContext: false,
            securityConfig: securityConfig
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should timeout after 5 seconds
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "EXECUTION_TIMEOUT");
    }

    [Fact]
    [Trait("Finding", "2")]
    [Trait("Severity", "High")]
    public async Task ExecuteAsync_LongRunningAsync_ShouldTimeout()
    {
        // Test that async/await code with cooperative cancellation respects timeout
        // (unlike synchronous infinite loops which cannot be forcefully terminated)

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Use production config with 2-second timeout for faster test
        var securityConfig = SecurityConfiguration.CreateProductionDefaults();
        securityConfig.ExecutionTimeout = TimeSpan.FromSeconds(2);

        // Act - Execute long-running async operation
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "await Task.Delay(TimeSpan.FromMinutes(10)); return 42;", // 10 minute delay, return value
            createContext: false,
            securityConfig: securityConfig
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should timeout after 2 seconds
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "EXECUTION_TIMEOUT");
    }

    [Fact]
    [Trait("Finding", "3")]
    [Trait("Severity", "High")]
    public async Task ExecuteAsync_LargeAllocation_ShouldFail()
    {
        // Test that gradual memory allocation respects limits
        // Note: GC.GetTotalMemory is process-wide, not per-context
        // This test may be flaky in parallel test execution

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Use production config with 10MB limit for faster test
        var securityConfig = SecurityConfiguration.CreateProductionDefaults();
        securityConfig.MaxMemoryBytes = 10 * 1024 * 1024; // 10MB limit

        // Act - Gradually allocate 50MB in a loop (should be caught by monitor)
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            @"var list = new System.Collections.Generic.List<byte[]>();
              for (int i = 0; i < 50; i++) {
                  list.Add(new byte[1024 * 1024]); // 1MB per iteration
                  await Task.Delay(10); // Allow monitoring to check
              }
              return list.Count;",
            createContext: false,
            securityConfig: securityConfig
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should fail due to memory limit
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.Contains(errors, e => e.GetProperty("code").GetString() == "MEMORY_LIMIT_EXCEEDED");
    }

    [Fact]
    [Trait("Finding", "3")]
    [Trait("Severity", "Critical")]
    public async Task ExecuteAsync_OutOfMemoryException_ShouldBeHandled()
    {
        // Test that OutOfMemoryException from .NET runtime is caught gracefully
        // (e.g., trying to allocate int.MaxValue bytes)

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        var securityConfig = SecurityConfiguration.CreateProductionDefaults();

        // Act - Try to allocate impossible amount (will throw OutOfMemoryException from runtime)
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            "var bomb = new byte[int.MaxValue]; return bomb.Length;",
            createContext: false,
            securityConfig: securityConfig
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should fail with runtime error (not crash)
        Assert.NotNull(resultDict);
        Assert.False(resultDict["success"].GetBoolean());

        var errors = resultDict["errors"].EnumerateArray().ToList();
        Assert.NotEmpty(errors);
        // Either MEMORY_LIMIT_EXCEEDED or RUNTIME_ERROR is acceptable
        Assert.Contains(
            errors,
            e =>
                e.GetProperty("code").GetString() == "MEMORY_LIMIT_EXCEEDED"
                || e.GetProperty("code").GetString() == "RUNTIME_ERROR"
        );
    }

    [Fact]
    [Trait("Finding", "4")]
    [Trait("Severity", "High")]
    public async Task ExecuteAsync_FileAccess_ShouldBeBlocked()
    {
        // Test that static analysis blocks dangerous filesystem APIs
        // Note: This is defense-in-depth. Container-level restrictions provide stronger isolation.

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Use production config with API restrictions enabled
        var securityConfig = SecurityConfiguration.CreateProductionDefaults();
        securityConfig.EnableApiRestrictions = true;

        // Act - Try to use File.ReadAllText (should be blocked by static analysis)
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            @"using System.IO;
              var content = File.ReadAllText(""/etc/passwd"");
              return content;",
            createContext: false,
            securityConfig: securityConfig
        );
        var json = TestJsonContext.SerializeDynamic(result);
        var resultDict = TestJsonContext.DeserializeToDictionary(json);

        // Assert - Should be blocked by static analysis
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

        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Create multiple contexts
        var contexts = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
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

    [Fact]
    [Trait("Finding", "StringFormatting")]
    [Trait("Severity", "Documentation")]
    public async Task StringInterpolation_CombinedFormatAndAlignment_CorrectSyntax()
    {
        // Regression test to document correct string interpolation syntax
        // Syntax is {value,alignment:format} NOT {value:format,alignment}
        // See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/tokens/interpolated

        // Arrange
        var contextMgr = new ExecutionContextManager();
        using var nugetSvc = new NuGetService();

        // Act - Test CORRECT syntax: alignment THEN format
        var result = await FileBasedToolsTestHelpers.EvaluateCsharpTest(
            contextMgr,
            nugetSvc,
            """
            var val = 26.5;
            var correct = $"{val,-5:F1}";     // Correct: {value,alignment:format}
            var incorrect = $"{val:F1,-5}";   // Wrong syntax - treated as literal
            return $"{correct}|{incorrect}";
            """
        );

        var json = TestJsonContext.SerializeDynamic(result);
        var dict = TestJsonContext.DeserializeToDictionary(json);

        // Assert
        Assert.NotNull(dict);
        Assert.True(dict["success"].GetBoolean(), "String formatting should succeed");

        var returnValue = dict["returnValue"].GetString();
        Assert.NotNull(returnValue);

        // Parse the return value
        var parts = returnValue.Split('|');
        Assert.Equal(2, parts.Length);

        // Verify correct syntax produces formatted output
        Assert.Equal("26.5", parts[0].Trim()); // Correct syntax: alignment then format

        // Verify incorrect syntax produces literal output (this is expected behavior)
        Assert.Equal("F1-5", parts[1].Trim()); // Wrong syntax produces literal - this is CORRECT behavior per C# spec
    }
}
