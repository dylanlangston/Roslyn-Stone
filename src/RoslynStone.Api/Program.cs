using RoslynStone.Infrastructure.Services;
using RoslynStone.Infrastructure.Tools;

// Determine transport mode from environment variable
// MCP_TRANSPORT: "stdio" (default) or "http"
var transportMode =
    Environment.GetEnvironmentVariable("MCP_TRANSPORT")?.ToLowerInvariant() ?? "stdio";
var useHttpTransport = transportMode == "http";

// Shared method to configure logging to stderr for both transport modes
static void ConfigureLogging(ILoggingBuilder logging)
{
    logging.ClearProviders();
    logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });
}

// Shared method to register all services
static void RegisterServices(IServiceCollection services)
{
    // Register services
    services.AddSingleton<RoslynScriptingService>();
    services.AddSingleton<DocumentationService>();
    services.AddSingleton<CompilationService>();
    services.AddSingleton<AssemblyExecutionService>();
    services.AddSingleton<NuGetService>();
}

if (useHttpTransport)
{
    // HTTP Transport Mode - Use WebApplication builder
    var builder = WebApplication.CreateBuilder(args);

    // Configure logging to stderr for consistency with stdio transport
    // This is a best practice even in HTTP mode
    ConfigureLogging(builder.Logging);

    // Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
    builder.AddServiceDefaults();

    // Register all services, command handlers, and query handlers
    RegisterServices(builder.Services);

    // WARNING: HTTP transport has no authentication by default.
    // Configure authentication, CORS, and rate limiting before exposing publicly.
    // This server can execute arbitrary C# code.
    builder
        .Services.AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly(typeof(ReplTools).Assembly);

    var app = builder.Build();

    // Map default health check endpoints for HTTP transport
    app.MapDefaultEndpoints();

    // WARNING: This endpoint allows code execution. Add authentication before exposing publicly.
    // Map MCP HTTP endpoints at /mcp
    app.MapMcp("/mcp");

    await app.RunAsync();
}
else
{
    // Stdio Transport Mode - Use generic Host builder
    var builder = Host.CreateApplicationBuilder(args);

    // Configure logging to stderr for consistency and to avoid interfering with stdio transport
    // This ensures MCP protocol integrity while preserving OpenTelemetry logging
    ConfigureLogging(builder.Logging);

    // Add Aspire service defaults (OpenTelemetry, health checks, service discovery)
    // This adds OpenTelemetry logging provider which will also log to stderr
    builder.AddServiceDefaults();

    // Register all services, command handlers, and query handlers
    RegisterServices(builder.Services);

    // Configure MCP server with stdio transport
    // Register tools from the Infrastructure assembly where the MCP tools are defined
    builder
        .Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly(typeof(ReplTools).Assembly);

    // Build and run the host
    await builder.Build().RunAsync();
}
