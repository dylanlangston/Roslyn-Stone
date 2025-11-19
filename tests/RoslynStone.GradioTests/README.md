# Gradio Landing Page Tests

This directory contains Playwright-based integration tests for the Gradio landing page.

## Prerequisites

- .NET 10 SDK
- Playwright (automatically installed via NuGet)

## Running the Tests

The tests are marked with `Skip` attribute by default because they require a running server instance. To run them:

### 1. Start the Server in HTTP Mode

```bash
cd src/RoslynStone.Api
MCP_TRANSPORT=http ASPNETCORE_URLS=http://localhost:7077 dotnet run
```

### 2. Run the Tests

In a separate terminal:

```bash
# Run all Gradio tests (after removing Skip attribute)
dotnet test tests/RoslynStone.GradioTests

# Or run specific tests
dotnet test tests/RoslynStone.GradioTests --filter "FullyQualifiedName~GradioLandingPage_LoadsSuccessfully"
```

### 3. Enable Tests for CI/CD

To enable these tests in CI/CD, remove the `Skip` parameter from the `[Fact]` attributes and ensure a server instance is running before the tests execute.

## Test Coverage

The tests verify:

- ✅ Landing page loads successfully
- ✅ Page displays expected heading and title
- ✅ All tabs are present (Quick Start, Features, Examples, About)
- ✅ Connection instructions are displayed
- ✅ Features tab lists MCP tools
- ✅ MCP endpoint remains accessible

## Playwright Installation

On first run, Playwright will automatically install Chromium browser. You can also manually install browsers:

```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
# or
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

## Configuration

- **Base URL**: `http://localhost:7077` (configured in `GradioLandingPageTests.cs`)
- **Timeout**: 30 seconds for page loads
- **Browser**: Chromium (headless mode)

## Test Structure

- `GradioLandingPageTests.cs`: Main test class with Playwright lifecycle management
- Tests use xUnit with `IAsyncLifetime` for proper async setup/teardown
- Tagged with traits: `Category=Integration`, `Component=Gradio`
