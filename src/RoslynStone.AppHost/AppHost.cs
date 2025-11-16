var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP server as a project resource
// The MCP server uses stdio transport and doesn't expose HTTP endpoints
// We'll configure it to be containerized with OpenTelemetry support
var mcpServer = builder.AddProject<Projects.RoslynStone_Api>("roslyn-stone-mcp")
    .WithEnvironment("OTEL_SERVICE_NAME", "roslyn-stone-mcp")
    .PublishAsDockerFile();

builder.Build().Run();
