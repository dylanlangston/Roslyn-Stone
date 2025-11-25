using Microsoft.Playwright;

namespace RoslynStone.GradioTests;

/// <summary>
/// Shared fixture for Playwright browser instance.
/// Initializes browser once and reuses it across all Playwright tests.
/// Separate from McpServerFixture to avoid blocking HttpClient-only tests.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;

    /// <summary>
    /// The browser instance shared across all tests in the collection
    /// </summary>
    public IBrowser? Browser { get; private set; }

    async Task IAsyncLifetime.InitializeAsync()
    {
        try
        {
            Console.WriteLine("[PLAYWRIGHT FIXTURE] Initializing Playwright...");

            // Initialize Playwright
            _playwright = await Playwright.CreateAsync();

            Console.WriteLine("[PLAYWRIGHT FIXTURE] Launching browser...");

            // Launch browser with container-friendly settings
            Browser = await _playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--disable-software-rasterizer",
                        "--disable-extensions",
                        "--disable-background-networking",
                        "--disable-sync",
                        "--no-first-run",
                        "--no-default-browser-check",
                    },
                    Timeout = 60000, // 60 second timeout for browser launch
                }
            );

            Console.WriteLine("[PLAYWRIGHT FIXTURE] Browser initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYWRIGHT FIXTURE] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[PLAYWRIGHT FIXTURE] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Console.WriteLine("[PLAYWRIGHT FIXTURE] Cleaning up...");

        if (Browser != null)
        {
            try
            {
                await Browser.CloseAsync();
                await Browser.DisposeAsync();
                Console.WriteLine("[PLAYWRIGHT FIXTURE] Browser closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAYWRIGHT FIXTURE] Error closing browser: {ex.Message}");
            }
        }

        _playwright?.Dispose();
        Console.WriteLine("[PLAYWRIGHT FIXTURE] Cleanup complete");
    }
}
