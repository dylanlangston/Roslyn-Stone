using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoslynStone.Core.Commands;
using RoslynStone.Core.CQRS;
using RoslynStone.Core.Models;
using RoslynStone.Core.Queries;
using RoslynStone.Infrastructure.CommandHandlers;
using RoslynStone.Infrastructure.QueryHandlers;
using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

// Determine transport mode from environment variable
// MCP_TRANSPORT: "stdio" (default) or "http"
var transportMode = Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.ToLowerInvariant() ?? "stdio";
var useHttpTransport = transportMode == "http";

if (useHttpTransport)
{
    // HTTP Transport Mode - Use WebApplication builder
    var builder = WebApplication.CreateBuilder(args);

    // Configure logging to stderr to avoid interfering with stdout
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    // Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
    builder.AddServiceDefaults();

    // Register services
    builder.Services.AddSingleton<RoslynScriptingService>();
    builder.Services.AddSingleton<DocumentationService>();
    builder.Services.AddSingleton<CompilationService>();
    builder.Services.AddSingleton<AssemblyExecutionService>();
    builder.Services.AddSingleton<NuGetService>();

    // Register command handlers
    builder.Services.AddSingleton<
        ICommandHandler<LoadPackageCommand, PackageReference>,
        LoadPackageCommandHandler
    >();

    // Register query handlers
    builder.Services.AddSingleton<
        IQueryHandler<SearchPackagesQuery, PackageSearchResult>,
        SearchPackagesQueryHandler
    >();
    builder.Services.AddSingleton<
        IQueryHandler<GetPackageVersionsQuery, List<PackageVersion>>,
        GetPackageVersionsQueryHandler
    >();
    builder.Services.AddSingleton<
        IQueryHandler<GetPackageReadmeQuery, string?>,
        GetPackageReadmeQueryHandler
    >();

    // Configure MCP server with HTTP transport
    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly(typeof(ReplTools).Assembly);

    var app = builder.Build();

    // Map default health check endpoints (only in development)
    app.MapDefaultEndpoints();

    // Map MCP HTTP endpoints at /mcp
    app.MapMcp("/mcp");

    await app.RunAsync();
}
else
{
    // Stdio Transport Mode - Use generic Host builder
    var builder = Host.CreateApplicationBuilder(args);

    // Configure logging to stderr BEFORE adding service defaults to avoid interfering with stdio transport
    // This ensures MCP protocol integrity while preserving OpenTelemetry logging
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    // Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
    // This adds OpenTelemetry logging provider which will also log to stderr
    builder.AddServiceDefaults();

    // Register services
    builder.Services.AddSingleton<RoslynScriptingService>();
    builder.Services.AddSingleton<DocumentationService>();
    builder.Services.AddSingleton<CompilationService>();
    builder.Services.AddSingleton<AssemblyExecutionService>();
    builder.Services.AddSingleton<NuGetService>();

    // Register command handlers
    builder.Services.AddSingleton<
        ICommandHandler<LoadPackageCommand, PackageReference>,
        LoadPackageCommandHandler
    >();

    // Register query handlers
    builder.Services.AddSingleton<
        IQueryHandler<SearchPackagesQuery, PackageSearchResult>,
        SearchPackagesQueryHandler
    >();
    builder.Services.AddSingleton<
        IQueryHandler<GetPackageVersionsQuery, List<PackageVersion>>,
        GetPackageVersionsQueryHandler
    >();
    builder.Services.AddSingleton<
        IQueryHandler<GetPackageReadmeQuery, string?>,
        GetPackageReadmeQueryHandler
    >();

    // Configure MCP server with stdio transport
    // Register tools from the Infrastructure assembly where the MCP tools are defined
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(ReplTools).Assembly);

    // Build and run the host
    await builder.Build().RunAsync();
}
