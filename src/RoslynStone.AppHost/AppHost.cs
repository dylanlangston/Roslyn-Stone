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

builder.Build().Run();
