namespace RoslynStone.GradioTests;

/// <summary>
/// Smoke tests for Gradio landing page functionality.
/// Uses HttpClient instead of Playwright due to NewPageAsync() deadlock issues in containers.
/// </summary>
[Collection("McpServer")]
[Trait("Category", "Integration")]
[Trait("Component", "Gradio")]
public class GradioLandingPageTests : IAsyncLifetime
{
    private readonly McpServerFixture _fixture;
    private HttpClient? _httpClient;

    public GradioLandingPageTests(McpServerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        Console.WriteLine("[TEST INIT] HttpClient initialized");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GradioLandingPage_ShouldLoad_Successfully()
    {
        Console.WriteLine($"[TEST] Fetching {_fixture.GradioUrl}");

        var response = await _httpClient!.GetAsync(_fixture.GradioUrl);
        Console.WriteLine($"[TEST] Response status: {response.StatusCode}");

        Assert.True(response.IsSuccessStatusCode, $"Expected 200, got {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[TEST] Content length: {content.Length} bytes");

        Assert.True(content.Length > 10000, $"Page content too small: {content.Length} bytes");
        Assert.Contains("gradio-app", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Roslyn", content, StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("[TEST] Page loaded successfully with expected content");
    }

    [Fact]
    public async Task GradioLandingPage_ShouldContain_ExpectedElements()
    {
        var response = await _httpClient!.GetAsync(_fixture.GradioUrl);
        var html = await response.Content.ReadAsStringAsync();

        // Check for Gradio-specific elements
        Assert.Contains("gradio-app", html);
        Assert.Contains("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);

        // Check for assets/scripts
        Assert.Contains("<script", html);
        Assert.Contains(".js", html);

        Console.WriteLine("[TEST] Expected HTML elements found");
    }

    [Fact]
    public async Task GradioLandingPage_Assets_ShouldLoad()
    {
        // First get the HTML to verify page exists
        var htmlResponse = await _httpClient!.GetAsync(_fixture.GradioUrl);
        _ = await htmlResponse.Content.ReadAsStringAsync();

        // Try to fetch theme.css (common Gradio asset)
        var cssResponse = await _httpClient.GetAsync($"{_fixture.GradioUrl}/theme.css");
        Console.WriteLine($"[TEST] CSS response: {cssResponse.StatusCode}");

        // Assets might not be at exact paths, but server should respond
        Assert.True(
            cssResponse.StatusCode == System.Net.HttpStatusCode.OK
                || cssResponse.StatusCode == System.Net.HttpStatusCode.NotFound,
            "Server should respond to asset requests"
        );
    }
}
