var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP server with stdio transport (default)
// The MCP server uses stdio transport by default and doesn't expose HTTP endpoints
// We'll configure it to be containerized with OpenTelemetry support
_ = builder
    .AddProject<Projects.RoslynStone_Api>("roslyn-stone-mcp-stdio")
    .WithEnvironment("MCP_TRANSPORT", "stdio")
    .WithEnvironment("OTEL_SERVICE_NAME", "roslyn-stone-mcp-stdio")
    .PublishAsDockerFile();

// Add the MCP server with HTTP transport
// This variant exposes HTTP endpoints for MCP protocol communication
// Make MCP HTTP endpoint port configurable via MCP_HTTP_PORT environment variable (default: 8080)
var mcpHttpPort = int.TryParse(builder.Configuration["MCP_HTTP_PORT"], out var httpPort)
    ? httpPort
    : 8080;
_ = builder
    .AddProject<Projects.RoslynStone_Api>("roslyn-stone-mcp-http")
    .WithEnvironment("MCP_TRANSPORT", "http")
    .WithEnvironment("OTEL_SERVICE_NAME", "roslyn-stone-mcp-http")
    .WithHttpEndpoint(port: mcpHttpPort, name: "mcp")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Add MCP Inspector for development/testing (only in development mode)
// The inspector provides a web UI for testing MCP tools
// Ports can be configured via environment variables: INSPECTOR_UI_PORT and INSPECTOR_PROXY_PORT
if (
    builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Development"
    || builder.Configuration["DOTNET_ENVIRONMENT"] == "Development"
    || string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_ENVIRONMENT"])
)
{
    var inspectorUiPort = int.TryParse(builder.Configuration["INSPECTOR_UI_PORT"], out var uiPort)
        ? uiPort
        : 6274;
    var inspectorProxyPort = int.TryParse(
        builder.Configuration["INSPECTOR_PROXY_PORT"],
        out var proxyPort
    )
        ? proxyPort
        : 6277;

    _ = builder
        .AddExecutable(
            "mcp-inspector",
            "npx",
            ".",
            "@modelcontextprotocol/inspector",
            "dotnet",
            "run",
            "--project",
            "src/RoslynStone.Api/RoslynStone.Api.csproj"
        )
        .WithHttpEndpoint(port: inspectorUiPort, name: "ui")
        .WithHttpEndpoint(port: inspectorProxyPort, name: "proxy")
        .WithExternalHttpEndpoints()
        .ExcludeFromManifest(); // Don't include in deployment manifest
}

builder.Build().Run();
