using RoslynStone.Core.Models;
using RoslynStone.Infrastructure.Models;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

namespace RoslynStone.Tests;

/// <summary>
/// Helper methods for calling FileBasedTools with proper dependencies in tests
/// Reduces boilerplate in test files after API signature changes
/// </summary>
public static class FileBasedToolsTestHelpers
{
    /// <summary>
    /// Call EvaluateCsharp with minimal required dependencies for testing
    /// </summary>
    public static Task<object> EvaluateCsharpTest(
        IExecutionContextManager contextManager,
        NuGetService nugetService,
        string code,
        string? contextId = null,
        NuGetPackageSpec[]? nugetPackages = null,
        bool createContext = false,
        SecurityConfiguration? securityConfig = null,
        CancellationToken cancellationToken = default
    )
    {
        // Create services for this test call
        securityConfig ??= SecurityConfiguration.CreateDevelopmentDefaults();
        var compilationService = new CompilationService();
        var isolatedExecutionService = new SessionIsolatedExecutionService(
            compilationService,
            securityConfig
        );

        // New signature: isolated execution only
        return FileBasedTools.EvaluateCsharp(
            isolatedExecutionService, // SessionIsolatedExecutionService
            contextManager, // IExecutionContextManager
            nugetService, // NuGetService
            securityConfig, // SecurityConfiguration
            compilationService, // CompilationService
            code,
            contextId,
            nugetPackages,
            createContext,
            cancellationToken
        );
    }

    /// <summary>
    /// Call ValidateCsharp with minimal required dependencies for testing
    /// </summary>
    public static Task<object> ValidateCsharpTest(
        IExecutionContextManager contextManager,
        NuGetService nugetService,
        string code,
        NuGetPackageSpec[]? nugetPackages = null,
        CancellationToken cancellationToken = default
    )
    {
        var compilationService = new CompilationService();
        var securityConfig = SecurityConfiguration.CreateDevelopmentDefaults();

        return FileBasedTools.ValidateCsharp(
            compilationService,
            nugetService,
            securityConfig,
            code,
            nugetPackages,
            cancellationToken
        );
    }
}
