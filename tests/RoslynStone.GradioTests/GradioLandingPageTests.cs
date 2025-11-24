using Microsoft.Playwright;

namespace RoslynStone.GradioTests;

/// <summary>
/// Playwright tests for the Gradio landing page.
/// These tests verify that the Gradio UI is accessible and displays the expected content.
/// Tests run as part of the standard CI pipeline to ensure UI functionality.
/// </summary>
public class GradioLandingPageTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string BaseUrl = "http://localhost:7077"; // Test server URL

    public async Task InitializeAsync()
    {
        // Assumes Playwright browsers are already installed (see README for setup instructions)
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
        _playwright?.Dispose();
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_LoadsSuccessfully()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            var response = await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.Ok,
                $"Page failed to load: {response.Status} {response.StatusText}"
            );

            // Wait for Gradio to fully load by waiting for specific element
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Verify page title
            var title = await page.TitleAsync();
            Assert.Equal("Roslyn-Stone MCP Testing UI", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_DisplaysExpectedHeading()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for specific element instead of fixed delay
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Assert - Check for main heading
            var heading = await page.Locator("h1").First.TextContentAsync();
            Assert.Contains("Roslyn-Stone MCP Server", heading);
            Assert.Contains("Interactive Testing UI", heading);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ContainsTabs()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for tabs to appear
            await page.WaitForSelectorAsync("button, [role='tab']", new() { Timeout = 5000 });

            // Assert - Check for tabs (Tools, Resources, Prompts, About)
            var tabs = await page.Locator("button, [role='tab']").AllTextContentsAsync();
            Assert.Contains(tabs, t => t.Contains("Tools"));
            Assert.Contains(tabs, t => t.Contains("Resources"));
            Assert.Contains(tabs, t => t.Contains("Prompts"));
            Assert.Contains(tabs, t => t.Contains("About"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ToolsTab_HasRefreshButton()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Assert - Check for Refresh Tools button
            var refreshButton = page.Locator("button:has-text('Refresh Tools')");
            await refreshButton.WaitForAsync(new() { Timeout = 5000 });
            Assert.True(await refreshButton.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ResourcesTab_HasRefreshButton()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Click on Resources tab
            var resourcesTab = page.Locator(
                "button:has-text('Resources'), [role='tab']:has-text('Resources')"
            ).First;
            await resourcesTab.ClickAsync();

            // Wait for tab content to load
            await page.WaitForSelectorAsync("button:has-text('Refresh Resources')", new() { Timeout = 5000 });

            // Assert - Check for Refresh Resources button
            var refreshButton = page.Locator("button:has-text('Refresh Resources')");
            Assert.True(await refreshButton.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_PromptsTab_HasRefreshButton()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Click on Prompts tab
            var promptsTab = page.Locator(
                "button:has-text('Prompts'), [role='tab']:has-text('Prompts')"
            ).First;
            await promptsTab.ClickAsync();

            // Wait for tab content to load
            await page.WaitForSelectorAsync("button:has-text('Refresh Prompts')", new() { Timeout = 5000 });

            // Assert - Check for Refresh Prompts button
            var refreshButton = page.Locator("button:has-text('Refresh Prompts')");
            Assert.True(await refreshButton.IsVisibleAsync());
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ToolsTab_HasExampleAccordion()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Assert - Check for Example Tool Calls accordion
            var accordion = page.Locator("button:has-text('Example Tool Calls')");
            await accordion.WaitForAsync(new() { Timeout = 5000 });
            Assert.True(await accordion.IsVisibleAsync());

            // Check that examples contain EvaluateCsharp, ValidateCsharp, SearchNuGetPackages
            var pageContent = await page.ContentAsync();
            Assert.Contains("EvaluateCsharp", pageContent);
            Assert.Contains("ValidateCsharp", pageContent);
            Assert.Contains("SearchNuGetPackages", pageContent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ResourcesTab_HasExampleAccordion()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Click on Resources tab
            var resourcesTab = page.Locator(
                "button:has-text('Resources'), [role='tab']:has-text('Resources')"
            ).First;
            await resourcesTab.ClickAsync();

            // Wait for tab content to load
            await page.WaitForSelectorAsync("button:has-text('Example Resource URIs')", new() { Timeout = 5000 });

            // Assert - Check for Example Resource URIs accordion
            var accordion = page.Locator("button:has-text('Example Resource URIs')");
            Assert.True(await accordion.IsVisibleAsync());

            // Check that examples contain doc://, nuget://, repl:// URIs
            var pageContent = await page.ContentAsync();
            Assert.Contains("doc://", pageContent);
            Assert.Contains("nuget://", pageContent);
            Assert.Contains("repl://", pageContent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_StatusIndicator_IsVisible()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(
                BaseUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 }
            );

            // Wait for page to load
            await page.WaitForSelectorAsync("h1", new() { Timeout = 5000 });

            // Assert - Check for status indicator
            var pageContent = await page.ContentAsync();
            Assert.Contains("Status", pageContent);
            Assert.Contains("Connected to MCP server", pageContent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task McpEndpoint_ReturnsValidResponse()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act - Test that /mcp endpoint is still accessible
            var response = await page.GotoAsync(
                $"{BaseUrl}/mcp",
                new PageGotoOptions { Timeout = 10000 }
            );

            // Assert
            Assert.NotNull(response);
            // MCP endpoint should return something (could be error without POST, but should respond)
            Assert.True(response.Status != 404, "MCP endpoint should exist");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
