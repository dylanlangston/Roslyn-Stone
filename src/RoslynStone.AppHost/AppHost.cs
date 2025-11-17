var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP server with HTTP transport
// This variant exposes HTTP endpoints for MCP protocol communication
// Make MCP HTTP endpoint port configurable via MCP_HTTP_PORT environment variable (default: 8080)
var mcpHttpPort = int.TryParse(builder.Configuration["MCP_HTTP_PORT"], out var httpPort)
    ? httpPort
    : 8080;
var mcpServer = builder
    .AddProject<Projects.RoslynStone_Api>("roslyn-stone-mcp")
    .WithEnvironment("MCP_TRANSPORT", "http")
    .WithEnvironment("OTEL_SERVICE_NAME", "roslyn-stone-mcp")
    .WithHttpEndpoint(port: mcpHttpPort, name: "mcp")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

// Add MCP Inspector for development/testing (only in development mode)
// The inspector provides a web UI for testing MCP tools via SSE transport
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

    // Get the MCP server endpoint reference
    var mcpEndpoint = mcpServer.GetEndpoint("mcp");

    _ = builder
        .AddExecutable(
            "mcp-inspector",
            "npx",
            ".",
            "@modelcontextprotocol/inspector",
            "-t",
            "sse"
        )
        .WithEnvironment(context =>
        {
            // Pass the MCP server SSE endpoint URL to the inspector
            // This will be resolved at runtime when the endpoint is allocated
            context.EnvironmentVariables["MCP_SERVER_URL"] = mcpEndpoint.Property(EndpointProperty.Url);
        })
        .WithArgs("-u", "{MCP_SERVER_URL}/mcp/sse")
        .WithHttpEndpoint(port: inspectorUiPort, name: "ui")
        .WithHttpEndpoint(port: inspectorProxyPort, name: "proxy")
        .WithExternalHttpEndpoints()
        .ExcludeFromManifest(); // Don't include in deployment manifest
}

builder.Build().Run();
