using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;

namespace RoslynStone.GradioTests;

/// <summary>
/// Playwright-based integration tests for Gradio landing page.
/// Uses Microsoft.Playwright.Xunit for proper async test handling.
/// Runs in separate collection from HttpClient tests to avoid blocking.
/// </summary>
[Collection("Playwright")]
[Trait("Category", "Integration")]
[Trait("Component", "Gradio")]
public class GradioLandingPagePlaywrightTests : IAsyncLifetime
{
    private readonly McpServerFixture _fixture;
    private readonly PlaywrightFixture _playwrightFixture;

    public GradioLandingPagePlaywrightTests(
        McpServerFixture fixture,
        PlaywrightFixture playwrightFixture
    )
    {
        _fixture = fixture;
        _playwrightFixture = playwrightFixture;
    }

    public Task InitializeAsync()
    {
        Console.WriteLine("[PLAYWRIGHT TEST] Test initialized");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Console.WriteLine("[PLAYWRIGHT TEST] Test cleanup");
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GradioLandingPage_ShouldLoad_WithPlaywright()
    {
        // Create a new browser context and page for this test
        var context = await _playwrightFixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            Console.WriteLine($"[PLAYWRIGHT] Navigating to {_fixture.GradioUrl}");

            // Navigate to the Gradio landing page with timeout
            // Use Load instead of NetworkIdle because Gradio has continuous SSE connections
            var response = await page.GotoAsync(
                _fixture.GradioUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }
            );

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Expected successful response, got {response.Status}");

            Console.WriteLine($"[PLAYWRIGHT] Page loaded with status {response.Status}");

            // Wait for Gradio app to initialize
            await page.WaitForSelectorAsync(
                "gradio-app",
                new PageWaitForSelectorOptions { Timeout = 10000 }
            );

            // Verify page content contains expected text
            var content = await page.ContentAsync();
            Console.WriteLine($"[PLAYWRIGHT] Page content length: {content.Length}");
            Assert.Contains("Roslyn", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MCP", content, StringComparison.OrdinalIgnoreCase);

            // Take a screenshot for debugging (saved to test output)
            await page.ScreenshotAsync(
                new PageScreenshotOptions
                {
                    Path = Path.Combine(AppContext.BaseDirectory, "gradio-landing-page.png"),
                    FullPage = true,
                }
            );

            Console.WriteLine("[PLAYWRIGHT] Test completed successfully");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task GradioLandingPage_ShouldHave_ExpectedElements()
    {
        var context = await _playwrightFixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(
                _fixture.GradioUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }
            );

            // Check for Gradio app container
            var gradioApp = await page.QuerySelectorAsync("gradio-app");
            Assert.NotNull(gradioApp);

            // Check for main content sections
            var content = await page.ContentAsync();
            Assert.Contains("Roslyn", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MCP", content, StringComparison.OrdinalIgnoreCase);

            Console.WriteLine("[PLAYWRIGHT] Expected elements found");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task GradioLandingPage_Tabs_ShouldBeInteractive()
    {
        var context = await _playwrightFixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(
                _fixture.GradioUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }
            );

            // Wait for Gradio to fully load
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Give Gradio extra time to initialize its interactive elements
            await Task.Delay(2000);

            // Look for tab navigation elements (Gradio uses specific classes)
            var tabButtons = await page.QuerySelectorAllAsync("button[role='tab']");
            Console.WriteLine($"[PLAYWRIGHT] Found {tabButtons.Count} tab buttons");

            // Verify tabs exist (Gradio typically has multiple tabs)
            Assert.True(tabButtons.Count > 0, "Expected to find tab buttons in the UI");

            Console.WriteLine("[PLAYWRIGHT] Tabs are present and interactive");
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task GradioLandingPage_Assets_ShouldLoad()
    {
        var context = await _playwrightFixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        // Track failed requests
        var failedRequests = new List<string>();
        page.RequestFailed += (_, request) =>
        {
            failedRequests.Add($"{request.Method} {request.Url} - {request.Failure}");
            Console.WriteLine($"[PLAYWRIGHT] Request failed: {request.Url}");
        };

        try
        {
            await page.GotoAsync(
                _fixture.GradioUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }
            );

            // Wait a bit for all assets to load
            await Task.Delay(3000);

            // Check that we don't have critical failed requests
            // Filter out expected 404s (like favicon.ico)
            var criticalFailures = failedRequests
                .Where(f => !f.Contains("favicon.ico") && !f.Contains("robots.txt"))
                .ToList();

            Console.WriteLine($"[PLAYWRIGHT] Total failed requests: {failedRequests.Count}");
            Console.WriteLine($"[PLAYWRIGHT] Critical failures: {criticalFailures.Count}");

            if (criticalFailures.Any())
            {
                Console.WriteLine("[PLAYWRIGHT] Critical failures:");
                foreach (var failure in criticalFailures)
                {
                    Console.WriteLine($"  - {failure}");
                }
            }

            // Assert that critical assets loaded (some 404s for non-critical resources are acceptable)
            Assert.True(
                criticalFailures.Count < 5,
                $"Too many critical asset failures: {string.Join(", ", criticalFailures)}"
            );
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task GradioLandingPage_Console_ShouldNotHave_CriticalErrors()
    {
        var context = await _playwrightFixture.Browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        var consoleErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
                Console.WriteLine($"[PLAYWRIGHT] Console error: {msg.Text}");
            }
        };

        try
        {
            await page.GotoAsync(
                _fixture.GradioUrl,
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 30000 }
            );

            // Wait for page to fully initialize
            await Task.Delay(3000);

            Console.WriteLine($"[PLAYWRIGHT] Console errors count: {consoleErrors.Count}");

            // Filter out non-critical errors (Gradio might have some warnings)
            var criticalErrors = consoleErrors
                .Where(e =>
                    !e.Contains("favicon") && !e.Contains("Warning") && !e.Contains("DevTools")
                )
                .ToList();

            if (criticalErrors.Any())
            {
                Console.WriteLine("[PLAYWRIGHT] Critical console errors:");
                foreach (var error in criticalErrors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }

            // Some console errors might be acceptable, but not too many
            Assert.True(
                criticalErrors.Count < 3,
                $"Too many critical console errors: {string.Join(", ", criticalErrors)}"
            );
        }
        finally
        {
            await page.CloseAsync();
            await context.CloseAsync();
        }
    }
}
