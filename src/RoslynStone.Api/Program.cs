using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoslynStone.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr to avoid interfering with stdio transport
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => 
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register services
builder.Services.AddSingleton<RoslynScriptingService>();
builder.Services.AddSingleton<DocumentationService>();

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build and run the host
await builder.Build().RunAsync();

