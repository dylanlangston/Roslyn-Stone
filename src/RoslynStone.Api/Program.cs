using CSnakes.Runtime;
using RoslynStone.Infrastructure.Resources;
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
    services.AddSingleton<IReplContextManager, ReplContextManager>();
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

    // Configure CSnakes Python environment with UV for Gradio
    var pythonHome = AppContext.BaseDirectory; // Python files are copied to output from GradioModule
    var venvPath = Path.Combine(pythonHome, ".venv");
    
    // Set LD_LIBRARY_PATH to include Python shared library location from redistributable
    var csnakesPythonPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "CSnakes", "python3.12.9", "python", "install", "lib");
    var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
    if (string.IsNullOrEmpty(currentLdPath))
    {
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", csnakesPythonPath);
    }
    else if (!currentLdPath.Contains(csnakesPythonPath))
    {
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{csnakesPythonPath}:{currentLdPath}");
    }
    
    // Set PATH to include UV location
    var uvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");
    var currentPath = Environment.GetEnvironmentVariable("PATH");
    if (!string.IsNullOrEmpty(currentPath) && !currentPath.Contains(uvPath))
    {
        Environment.SetEnvironmentVariable("PATH", $"{uvPath}:{currentPath}");
    }
    
    builder.Services
        .WithPython()
        .WithHome(pythonHome)
        .WithVirtualEnvironment(venvPath)
        .FromRedistributable()  // Use Python from CSnakes redistributable
        .WithUvInstaller("pyproject.toml");  // Use UV to install from pyproject.toml

    builder.Services.AddHttpClient();

    // WARNING: HTTP transport has no authentication by default.
    // Configure authentication, CORS, and rate limiting before exposing publicly.
    // This server can execute arbitrary C# code.
    builder
        .Services.AddMcpServer()
        .WithHttpTransport()
        .WithPromptsFromAssembly(typeof(GuidancePrompts).Assembly)
        .WithToolsFromAssembly(typeof(ReplTools).Assembly)
        .WithResourcesFromAssembly(typeof(DocumentationResource).Assembly);

    var app = builder.Build();

    // Start Gradio landing page using CSnakes in the background
    _ = Task.Run(async () =>
    {
        try
        {
            var env = app.Services.GetRequiredService<IPythonEnvironment>();
            var gradioLauncher = env.GradioLauncher();
            
            var baseUrl = app.Configuration["BASE_URL"] ?? "http://localhost:7071";
            
            // Check if Gradio is installed
            var isInstalled = gradioLauncher.CheckGradioInstalled();
            if (!isInstalled)
            {
                app.Logger.LogWarning("Gradio is not installed in the Python environment");
                return;
            }
            
            // Start Gradio server (runs in a thread inside Python)
            var result = gradioLauncher.StartGradioServer(baseUrl, 7860);
            app.Logger.LogInformation("Gradio landing page: {Result}", result);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to start Gradio landing page");
        }
    });

    // Proxy root path to Gradio
    app.MapGet("/", async (IHttpClientFactory clientFactory) =>
    {
        var client = clientFactory.CreateClient();
        try
        {
            var response = await client.GetAsync("http://127.0.0.1:7860/");
            return Results.Content(
                await response.Content.ReadAsStringAsync(),
                response.Content.Headers.ContentType?.ToString()
            );
        }
        catch (Exception)
        {
            // If Gradio isn't running, redirect to MCP endpoint
            return Results.Redirect("/mcp");
        }
    });

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
        .WithPromptsFromAssembly(typeof(GuidancePrompts).Assembly)
        .WithToolsFromAssembly(typeof(ReplTools).Assembly)
        .WithResourcesFromAssembly(typeof(DocumentationResource).Assembly);

    // Build and run the host
    await builder.Build().RunAsync();
}
