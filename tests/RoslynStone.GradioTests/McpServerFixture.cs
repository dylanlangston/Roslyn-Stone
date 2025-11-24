using System.Diagnostics;
using System.Net;
using System.Text;
using Xunit.Abstractions;

namespace RoslynStone.GradioTests;

/// <summary>
/// Production-quality test fixture for managing the RoslynStone.Api MCP server lifecycle.
/// Handles server startup, readiness detection (including Gradio UI), and proper cleanup.
/// </summary>
public sealed class McpServerFixture : IAsyncDisposable
{
    private readonly ITestOutputHelper? _output;
    private Process? _serverProcess;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;
    private readonly StringBuilder _serverOutput = new();
    private readonly StringBuilder _serverError = new();

    public string BaseUrl { get; }
    public int ServerStartupTimeoutSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the McpServerFixture.
    /// </summary>
    /// <param name="output">xUnit test output helper for diagnostics.</param>
    /// <param name="baseUrl">The base URL for the server (default: http://localhost:7077).</param>
    /// <param name="serverStartupTimeoutSeconds">Timeout in seconds for server startup (default: 60).</param>
    public McpServerFixture(
        ITestOutputHelper? output = null,
        string baseUrl = "http://localhost:7077",
        int serverStartupTimeoutSeconds = 60
    )
    {
        _output = output;
        BaseUrl = baseUrl;
        ServerStartupTimeoutSeconds = serverStartupTimeoutSeconds;
    }

    /// <summary>
    /// Initializes the server. This method is thread-safe and can be called multiple times.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await StartServerAsync(CancellationToken.None).ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task StartServerAsync(CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        WriteMessage("Starting MCP server...");

        try
        {
            // Find the compiled API DLL using build output from the test project
            var apiDllPath = FindApiDllPath();
            WriteMessage($"Using API DLL: {apiDllPath}");

            // Verify the DLL exists
            if (!File.Exists(apiDllPath))
            {
                throw new FileNotFoundException(
                    $"RoslynStone.Api.dll not found at: {apiDllPath}. "
                        + "Ensure the API project is built before running tests.",
                    apiDllPath
                );
            }

            // Parse BaseUrl to get components for environment variable
            var uri = new Uri(BaseUrl);

            // Configure the server process
            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"exec \"{apiDllPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(apiDllPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment =
                    {
                        ["MCP_TRANSPORT"] = "http",
                        ["ASPNETCORE_URLS"] = BaseUrl,
                        // Ensure Python output is not buffered for better diagnostics
                        ["PYTHONUNBUFFERED"] = "1",
                    },
                },
                EnableRaisingEvents = true,
            };

            // Capture server output for diagnostics
            _serverProcess.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _serverOutput.AppendLine(e.Data);
                    WriteMessage($"[SERVER OUT] {e.Data}");
                }
            };

            _serverProcess.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _serverError.AppendLine(e.Data);
                    WriteMessage($"[SERVER ERR] {e.Data}");
                }
            };

            _serverProcess.Exited += (_, _) =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    WriteMessage(
                        $"[WARNING] Server process exited unexpectedly with code: {_serverProcess.ExitCode}"
                    );
                }
            };

            // Start the process
            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

            WriteMessage($"Server process started with PID: {_serverProcess.Id}");

            // Wait for server to be ready (ASP.NET Core + Gradio)
            await WaitForServerReadyAsync(cancellationToken).ConfigureAwait(false);

            var elapsed = DateTime.UtcNow - startTime;
            WriteMessage($"Server is ready! Total startup time: {elapsed.TotalSeconds:F2}s");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WriteMessage($"Failed to start server: {ex.Message}");
            WriteMessage($"Server output:\n{_serverOutput}");
            WriteMessage($"Server errors:\n{_serverError}");

            // Clean up on failure
            await StopServerAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                $"Failed to start MCP server. Check test output for details. Error: {ex.Message}",
                ex
            );
        }
    }

    private string FindApiDllPath()
    {
        // The test project has a project reference to RoslynStone.Api,
        // so the API DLL will be in the test output directory
        var testAssemblyPath = typeof(McpServerFixture).Assembly.Location;
        var testOutputDir =
            Path.GetDirectoryName(testAssemblyPath)
            ?? throw new InvalidOperationException("Could not determine test output directory");

        var apiDllPath = Path.Combine(testOutputDir, "RoslynStone.Api.dll");

        WriteMessage($"Test assembly: {testAssemblyPath}");
        WriteMessage($"Test output directory: {testOutputDir}");
        WriteMessage($"Expected API DLL path: {apiDllPath}");

        return apiDllPath;
    }

    private async Task WaitForServerReadyAsync(CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(ServerStartupTimeoutSeconds)
        );
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token
        );

        WriteMessage("Waiting for ASP.NET Core server to be ready...");

        // Phase 1: Wait for ASP.NET Core to respond
        await WaitForHttpEndpointAsync(BaseUrl, linkedCts.Token).ConfigureAwait(false);
        WriteMessage("✓ ASP.NET Core server is responding");

        // Phase 2: Wait for Gradio UI to be fully initialized
        // Gradio takes additional time to start after ASP.NET Core is ready
        WriteMessage("Waiting for Gradio UI to initialize...");
        await WaitForGradioReadyAsync(linkedCts.Token).ConfigureAwait(false);
        WriteMessage("✓ Gradio UI is ready");
    }

    private async Task WaitForHttpEndpointAsync(string url, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var attempt = 0;
        var delay = 100; // Start with 100ms

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var response = await httpClient
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                // Accept any response (even 404) as long as the server is responding
                if (
                    response.StatusCode != HttpStatusCode.BadGateway
                    && response.StatusCode != HttpStatusCode.ServiceUnavailable
                )
                {
                    WriteMessage(
                        $"Server responded with status: {response.StatusCode} after {attempt} attempts"
                    );
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not ready yet
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout on individual request, continue polling
            }

            // Exponential backoff with max delay of 2 seconds
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            delay = Math.Min(delay * 2, 2000);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task WaitForGradioReadyAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var attempt = 0;
        var maxAttempts = 30; // 30 attempts with 1 second delay = 30 seconds max
        var delay = 1000; // Check every second

        while (!cancellationToken.IsCancellationRequested && attempt < maxAttempts)
        {
            attempt++;
            try
            {
                // Try to fetch the root page and check if Gradio content is present
                var response = await httpClient
                    .GetAsync(BaseUrl, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response
                        .Content.ReadAsStringAsync(cancellationToken)
                        .ConfigureAwait(false);

                    // Check for Gradio-specific content that indicates the UI is ready
                    if (
                        content.Contains("gradio", StringComparison.OrdinalIgnoreCase)
                        || content.Contains("Roslyn-Stone MCP", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        WriteMessage($"Gradio UI detected after {attempt} attempts");
                        // Give it one more second to fully stabilize
                        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Connection issues, continue waiting
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout on individual request, continue polling
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        // If we get here, Gradio didn't fully initialize, but ASP.NET is running
        // Log a warning but don't fail - some tests might still work
        WriteMessage($"[WARNING] Gradio UI may not be fully initialized after {attempt} attempts");
    }

    private async Task StopServerAsync()
    {
        if (_serverProcess == null)
        {
            return;
        }

        WriteMessage("Stopping MCP server...");
        var process = _serverProcess;
        _serverProcess = null;

        try
        {
            if (!process.HasExited)
            {
                // Kill the entire process tree to ensure Python/Gradio processes are terminated
                process.Kill(entireProcessTree: true);

                // Wait for graceful exit with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

                WriteMessage($"Server stopped gracefully. Exit code: {process.ExitCode}");
            }
            else
            {
                WriteMessage($"Server already exited with code: {process.ExitCode}");
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited
            WriteMessage("Server process already exited");
        }
        catch (OperationCanceledException)
        {
            // Force kill if graceful shutdown times out
            WriteMessage("[WARNING] Server did not stop gracefully, forcing termination");
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited during force kill
            }
        }
        catch (Exception ex)
        {
            WriteMessage($"[ERROR] Error stopping server: {ex.Message}");
        }
        finally
        {
            process.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopServerAsync().ConfigureAwait(false);
        _initializationLock.Dispose();
    }

    private void WriteMessage(string message)
    {
        var timestampedMessage = $"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}";

        // Write to test output if available, otherwise to console for CI diagnostics
        if (_output != null)
        {
            try
            {
                _output.WriteLine(timestampedMessage);
            }
            catch (InvalidOperationException)
            {
                // Test has completed, output is no longer available
                // Fall back to Console for diagnostics
                Console.WriteLine(timestampedMessage);
            }
        }
        else
        {
            Console.WriteLine(timestampedMessage);
        }
    }
}
