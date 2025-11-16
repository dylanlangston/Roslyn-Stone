var builder = DistributedApplication.CreateBuilder(args);

// Add the MCP server as a project resource
// The MCP server uses stdio transport and doesn't expose HTTP endpoints
// We'll configure it to be containerized with OpenTelemetry support
var mcpServer = builder.AddProject<Projects.RoslynStone_Api>("roslyn-stone-mcp")
    .WithEnvironment("OTEL_SERVICE_NAME", "roslyn-stone-mcp")
    .PublishAsDockerFile();

// Add MCP Inspector for development/testing (only in development mode)
// The inspector provides a web UI at http://localhost:6274 for testing MCP tools
if (builder.Configuration["ASPNETCORE_ENVIRONMENT"] == "Development" || 
    builder.Configuration["DOTNET_ENVIRONMENT"] == "Development" ||
    string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_ENVIRONMENT"]))
{
    var inspector = builder.AddExecutable("mcp-inspector", "npx", ".",
        "@modelcontextprotocol/inspector", 
        "dotnet", "run", "--project", "src/RoslynStone.Api/RoslynStone.Api.csproj")
        .WithHttpEndpoint(port: 6274, name: "ui")
        .WithHttpEndpoint(port: 6277, name: "proxy")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
