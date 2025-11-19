using Microsoft.Playwright;

namespace RoslynStone.GradioTests;

/// <summary>
/// Playwright tests for the Gradio landing page.
/// These tests verify that the Gradio UI is accessible and displays the expected content.
/// </summary>
public class GradioLandingPageTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private const string BaseUrl = "http://localhost:7077"; // Test server URL

    public async Task InitializeAsync()
    {
        // Install Playwright browsers if not already installed
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
        {
            throw new Exception($"Playwright install failed with exit code {exitCode}");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
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

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_LoadsSuccessfully()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            var response = await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // Assert
            Assert.NotNull(response);
            Assert.True(response.Ok, $"Page failed to load: {response.Status} {response.StatusText}");
            
            // Wait for Gradio to fully load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000); // Allow Gradio JS to initialize
            
            // Verify page title
            var title = await page.TitleAsync();
            Assert.Equal("Roslyn-Stone MCP Server", title);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_DisplaysExpectedHeading()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);

            // Assert - Check for main heading
            var heading = await page.Locator("h1").First.TextContentAsync();
            Assert.Contains("Roslyn-Stone MCP Server", heading);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_ContainsTabs()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);

            // Assert - Check for tabs
            var tabs = await page.Locator("button, [role='tab']").AllTextContentsAsync();
            Assert.Contains(tabs, t => t.Contains("Quick Start"));
            Assert.Contains(tabs, t => t.Contains("Features"));
            Assert.Contains(tabs, t => t.Contains("Examples"));
            Assert.Contains(tabs, t => t.Contains("About"));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_DisplaysConnectionInstructions()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);

            // Assert - Check for connection instructions
            var pageContent = await page.ContentAsync();
            Assert.Contains("Connect to This Server", pageContent);
            Assert.Contains("HTTP mode", pageContent);
            Assert.Contains("/mcp", pageContent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task GradioLandingPage_FeaturesTab_DisplaysToolsList()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act
            await page.GotoAsync(BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000);

            // Click on Features tab
            var featuresTab = page.Locator("button:has-text('Features'), [role='tab']:has-text('Features')").First;
            await featuresTab.ClickAsync();
            await Task.Delay(1000);

            // Assert - Check for MCP tools
            var pageContent = await page.ContentAsync();
            Assert.Contains("EvaluateCsharp", pageContent);
            Assert.Contains("ValidateCsharp", pageContent);
            Assert.Contains("LoadNuGetPackage", pageContent);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Fact(Skip = "Requires running server - enable for integration tests")]
    [Trait("Category", "Integration")]
    [Trait("Component", "Gradio")]
    public async Task McpEndpoint_ReturnsValidResponse()
    {
        // Arrange
        var page = await _browser!.NewPageAsync();

        try
        {
            // Act - Test that /mcp endpoint is still accessible
            var response = await page.GotoAsync($"{BaseUrl}/mcp", new PageGotoOptions
            {
                Timeout = 10000
            });

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
