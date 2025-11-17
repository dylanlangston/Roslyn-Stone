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
    // Add both a named 'mcp' endpoint (used by tests) and a default 'http' endpoint
    // so that the CommunityToolkit inspector can find the expected 'http' endpoint.
    // Create the standard 'http' endpoint for MCP servers (inspector expects endpoint name 'http')
    .WithHttpEndpoint(port: mcpHttpPort, name: "http")
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

    // Add the MCP Inspector as a managed AppHost resource instead of launching the JS inspector via npx.
    // Use server and client ports from configuration and attach to the MCP server created above.
    var inspector = builder.AddMcpInspector("mcp-inspector", options =>
    {
        options.ClientPort = inspectorUiPort;
        options.ServerPort = inspectorProxyPort;
        // Default inspector version will be used; override with InspectorVersion if needed.
    })
    // Connect the Inspector to the MCP server resource
    .WithMcpServer(mcpServer)
    // Expose the inspector's HTTP endpoints and mark it as a development-only resource
    .WithExternalHttpEndpoints()
    .ExcludeFromManifest();
}

builder.Build().Run();
