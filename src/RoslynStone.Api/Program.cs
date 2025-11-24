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

    // Dynamically determine Python version for CSnakes path
    var csnakesPythonVersion =
        Environment.GetEnvironmentVariable("CSNAKES_PYTHON_VERSION") ?? "3.12.9";
    var csnakesPythonPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "CSnakes",
        $"python{csnakesPythonVersion}",
        "python",
        "install",
        "lib"
    );
    var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
    if (string.IsNullOrEmpty(currentLdPath))
    {
        Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", csnakesPythonPath);
    }
    else if (!currentLdPath.Contains(csnakesPythonPath))
    {
        Environment.SetEnvironmentVariable(
            "LD_LIBRARY_PATH",
            $"{csnakesPythonPath}:{currentLdPath}"
        );
    }

    // Set PATH to include UV location
    var uvPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local",
        "bin"
    );
    var currentPath = Environment.GetEnvironmentVariable("PATH");
    if (!string.IsNullOrEmpty(currentPath) && !currentPath.Contains(uvPath))
    {
        Environment.SetEnvironmentVariable("PATH", $"{uvPath}:{currentPath}");
    }

    builder
        .Services.WithPython()
        .WithHome(pythonHome)
        .WithVirtualEnvironment(venvPath)
        .FromRedistributable() // Use Python from CSnakes redistributable
        .WithUvInstaller("pyproject.toml"); // Use UV to install from pyproject.toml

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

    // Get Gradio server port from configuration
    // In HuggingFace Spaces, the C# app runs on 7860, so Gradio must use a different port
    var isHuggingFaceSpace = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SPACE_ID"));
    var defaultGradioPort = isHuggingFaceSpace ? 7861 : 7860;
    var gradioPort = app.Configuration.GetValue<int>("GradioServerPort", defaultGradioPort);

    // Start Gradio landing page using CSnakes in the background
    _ = Task.Run(() =>
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
            var result = gradioLauncher.StartGradioServer(baseUrl, gradioPort);
            app.Logger.LogInformation("Gradio landing page: {Result}", result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.Logger.LogError(ex, "Failed to start Gradio landing page");
        }
    });

    // Proxy root path to Gradio
    app.MapGet(
        "/",
        async (HttpContext context, IHttpClientFactory clientFactory) =>
        {
            using var client = clientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            try
            {
                var response = await client.GetAsync($"http://127.0.0.1:{gradioPort}/");
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "text/html";

                // Whitelist of allowed content types for security
                var allowedContentTypes = new[]
                {
                    "text/html",
                    "text/css",
                    "application/javascript",
                    "application/json",
                    "text/plain",
                    "image/png",
                    "image/jpeg",
                    "image/gif",
                    "image/svg+xml",
                };
                if (
                    !allowedContentTypes.Any(allowed =>
                        contentType.Contains(allowed, StringComparison.OrdinalIgnoreCase)
                    )
                )
                {
                    contentType = "text/plain";
                }

                var content = await response.Content.ReadAsStringAsync();

                // Set security headers
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

                return Results.Content(content, contentType);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // If Gradio isn't running, redirect to MCP endpoint
                app.Logger.LogWarning(ex, "Failed to proxy to Gradio, redirecting to /mcp");
                return Results.Redirect("/mcp");
            }
        }
    );

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
