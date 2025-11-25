namespace RoslynStone.GradioTests;

/// <summary>
/// xUnit collection definition for Playwright tests.
/// This collection uses both McpServerFixture (for the running server)
/// and PlaywrightFixture (for the browser instance).
/// Separated from HttpClient-only tests to avoid blocking if Playwright setup fails.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection
    : ICollectionFixture<McpServerFixture>,
        ICollectionFixture<PlaywrightFixture>
{
    // This class has no code, and is never instantiated.
    // Its purpose is to be the place to apply [CollectionDefinition] and the fixtures.
}
