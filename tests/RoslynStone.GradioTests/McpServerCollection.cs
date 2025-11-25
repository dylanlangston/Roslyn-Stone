namespace RoslynStone.GradioTests;

/// <summary>
/// xUnit collection definition for sharing McpServerFixture across all Gradio tests.
/// This ensures the MCP server starts once and is reused for all tests in the collection.
/// </summary>
[CollectionDefinition("McpServer")]
public class McpServerCollection : ICollectionFixture<McpServerFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is to be the place to apply [CollectionDefinition] and the fixtures.
}
