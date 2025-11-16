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

var builder = Host.CreateApplicationBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Configure logging to stderr to avoid interfering with stdio transport
// This overrides the default console logging to ensure MCP protocol integrity
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

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
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

// Build and run the host
await builder.Build().RunAsync();
