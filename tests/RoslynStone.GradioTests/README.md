# Gradio Landing Page Tests

This directory contains Playwright-based integration tests for the Gradio landing page.

## Overview

The tests automatically start and stop the RoslynStone.Api server in HTTP MCP mode, verify the Gradio UI is ready, and run comprehensive UI tests. No manual server management is required.

## Prerequisites

- .NET 10 SDK
- Playwright (automatically installed via NuGet)
- Python 3.x (for Gradio - typically already installed)

## Running the Tests

The tests are fully automated with server lifecycle management:

```bash
# Run all Gradio tests
dotnet test tests/RoslynStone.GradioTests

# Run with verbose output
dotnet test tests/RoslynStone.GradioTests --logger "console;verbosity=detailed"

# Run specific test
dotnet test tests/RoslynStone.GradioTests --filter "FullyQualifiedName~GradioLandingPage_LoadsSuccessfully"

# Run tests by category
dotnet test tests/RoslynStone.GradioTests --filter "Category=Integration"
```

## Server Fixture Architecture

The test suite uses a production-quality `McpServerFixture` that provides:

### Key Features

1. **Reliable Server Startup**
   - Uses compiled DLL with `dotnet exec` (no rebuilding on each test run)
   - Automatic path resolution using project reference metadata
   - Configurable base URL and startup timeout
   - Thread-safe initialization with semaphore locking

2. **Smart Readiness Detection**
   - Phase 1: Polls HTTP endpoint with exponential backoff
   - Phase 2: Verifies Gradio UI is fully loaded (checks for Gradio-specific content)
   - Handles both ASP.NET Core and Python/Gradio initialization

3. **Comprehensive Diagnostics**
   - Captures and logs all server stdout/stderr output
   - Timestamped log messages for debugging
   - Outputs to xUnit test output helper (visible in test results)
   - Falls back to Console output for CI diagnostics

4. **Robust Cleanup**
   - Kills entire process tree (including child Python/Gradio processes)
   - Graceful shutdown with 5-second timeout
   - Force kill if graceful shutdown fails
   - Proper async disposal pattern with `IAsyncDisposable`

5. **Production-Ready Error Handling**
   - Detailed error messages with actionable context
   - Includes server logs in error output for debugging failures
   - No silent catch blocks - all exceptions are logged
   - Proper exception filtering (`catch (Exception) when (ex is not OperationCanceledException)`)

### Usage Example

```csharp
public class MyTests : IAsyncLifetime
{
    private readonly McpServerFixture _serverFixture;

    public MyTests(ITestOutputHelper output)
    {
        _serverFixture = new McpServerFixture(
            output: output,
            baseUrl: "http://localhost:7077",
            serverStartupTimeoutSeconds: 60
        );
    }

    public async Task InitializeAsync()
    {
        await _serverFixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _serverFixture.DisposeAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        // Server is already running at _serverFixture.BaseUrl
        using var client = new HttpClient();
        var response = await client.GetAsync(_serverFixture.BaseUrl);
        Assert.True(response.IsSuccessStatusCode);
    }
}
```

## Test Coverage

The tests verify:

- ✅ Landing page loads successfully
- ✅ Page displays expected heading and title ("Roslyn-Stone MCP Server Interactive Testing UI")
- ✅ All tabs are present (Tools, Resources, Prompts, Chat, About)
- ✅ Each tab has required elements (Refresh buttons, accordions)
- ✅ Status indicator shows "Connected to MCP server"
- ✅ Tools tab contains example tool calls (EvaluateCsharp, ValidateCsharp, etc.)
- ✅ Resources tab contains example resource URIs (doc://, nuget://, repl://)
- ✅ Chat tab has AI chat interface with LLM provider selection
- ✅ MCP endpoint remains accessible at `/mcp`

## Configuration

- **Base URL**: `http://localhost:7077` (configurable in fixture constructor)
- **Startup Timeout**: 60 seconds (configurable in fixture constructor)
- **Browser**: Chromium (headless mode)
- **Page Load Timeout**: 30 seconds
- **Environment**: Sets `MCP_TRANSPORT=http`, `ASPNETCORE_URLS`, and `PYTHONUNBUFFERED=1`

## Playwright Installation

On first run, Playwright will automatically install the Chromium browser. You can also manually install browsers:

```bash
# PowerShell
pwsh bin/Debug/net10.0/playwright.ps1 install

# Or using dotnet tool
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

## Test Structure

- `GradioLandingPageTests.cs`: Main test class with Playwright lifecycle management
- `McpServerFixture.cs`: Production-quality server lifecycle fixture
- Tests use xUnit with `IAsyncLifetime` for proper async setup/teardown
- Tagged with traits: `Category=Integration`, `Component=Gradio`

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:

- Server automatically starts and stops for each test run
- No manual server management required
- Comprehensive logging for diagnosing CI failures
- Process cleanup ensures no orphaned processes
- Hermetic tests that don't interfere with each other

## Troubleshooting

### Server Fails to Start

Check the test output for server logs. Common issues:

- **Port already in use**: Another process is using port 7077
  - Solution: Stop the conflicting process or change the port in the fixture
- **Python/Gradio not installed**: Gradio requires Python
  - Solution: Install Python 3.x and pip
- **Build artifacts missing**: API DLL not found
  - Solution: Run `dotnet build` before running tests

### Tests Timeout

If tests timeout during server startup:

- Check if Python/Gradio is slow to start (first run may be slower)
- Increase `serverStartupTimeoutSeconds` in fixture constructor
- Check test output for server error messages

### Process Cleanup Issues

If tests leave processes running:

- On Windows: Check Task Manager for `dotnet` or `python` processes
- On Linux/Mac: Use `ps aux | grep -E "dotnet|python"` to find orphaned processes
- The fixture uses `Kill(entireProcessTree: true)` which should handle this

## Development Notes

The fixture follows .NET best practices:

- Uses `ConfigureAwait(false)` for library-style async code
- Implements `IAsyncDisposable` for proper async cleanup
- Thread-safe initialization with `SemaphoreSlim`
- Exponential backoff for HTTP polling (reduces load)
- Proper cancellation token support throughout
- No silent catch blocks - all exceptions are logged
- Specific exception handling with `when` filters
