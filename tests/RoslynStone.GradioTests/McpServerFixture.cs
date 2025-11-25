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
    /// <summary>
    /// Timeout for ASP.NET Core to start accepting connections (seconds).
    /// This is usually fast (a few seconds).
    /// </summary>
    private const int AspNetCoreReadyTimeoutSeconds = 30;

    /// <summary>
    /// Timeout for Gradio to fully initialize after ASP.NET Core is ready (seconds).
    /// In CI, UV/Python setup can take significant time, so this needs to be generous.
    /// </summary>
    private const int GradioReadyTimeoutSeconds = 180;

    /// <summary>
    /// Delay between Gradio readiness polling attempts (milliseconds).
    /// Using a longer delay reduces log spam during the wait.
    /// </summary>
    private const int GradioPollingDelayMs = 3000;

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
                        // Allow pre-release packages for UV (gradio 6.0 needs gradio-client 2.0.0.dev3)
                        ["UV_PRERELEASE"] = "allow",
                        // Skip CSnakes Python install in CI when deps are pre-installed by CI workflow
                        ["SKIP_PYTHON_INSTALL"] = Environment.GetEnvironmentVariable("SKIP_PYTHON_INSTALL") ?? "false",
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
        // Phase 1: Wait for ASP.NET Core to accept connections (fast)
        WriteMessage(
            $"Phase 1: Waiting for ASP.NET Core server to accept connections (timeout: {AspNetCoreReadyTimeoutSeconds}s)..."
        );

        using var aspNetCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(AspNetCoreReadyTimeoutSeconds)
        );
        using var linkedAspNetCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            aspNetCts.Token
        );

        await WaitForHttpConnectionAsync(BaseUrl, linkedAspNetCts.Token).ConfigureAwait(false);
        WriteMessage("✓ ASP.NET Core server is accepting connections");

        // Phase 2: Wait for Gradio UI to be fully initialized (slow in CI)
        // In CI, UV needs to install Python dependencies which takes significant time
        WriteMessage(
            $"Phase 2: Waiting for Gradio UI to initialize (timeout: {GradioReadyTimeoutSeconds}s)..."
        );
        WriteMessage(
            "  Note: In CI, this may take several minutes while UV/Python dependencies are installed."
        );

        using var gradioCts = new CancellationTokenSource(
            TimeSpan.FromSeconds(GradioReadyTimeoutSeconds)
        );
        using var linkedGradioCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            gradioCts.Token
        );

        await WaitForGradioReadyAsync(linkedGradioCts.Token).ConfigureAwait(false);
        WriteMessage("✓ Gradio UI is ready");
    }

    /// <summary>
    /// Waits for the HTTP server to accept connections. This only verifies that ASP.NET Core
    /// is running and accepting requests - it does NOT verify that Gradio is ready.
    /// </summary>
    private async Task WaitForHttpConnectionAsync(string url, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var attempt = 0;
        var delay = 500; // Start with 500ms

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                // Any response (including 502) means ASP.NET Core is running
                var response = await httpClient
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                // 502/503 from YARP means Gradio isn't ready yet, but ASP.NET Core is
                WriteMessage(
                    $"  ASP.NET Core responded with status: {(int)response.StatusCode} after {attempt} attempts"
                );
                return;
            }
            catch (HttpRequestException ex)
            {
                // Connection refused - server not ready yet
                if (attempt % 5 == 1)
                {
                    // Log every 5th attempt to reduce noise
                    WriteMessage($"  Waiting for ASP.NET Core (attempt {attempt}): {ex.Message}");
                }
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout on individual request, continue polling
                if (attempt % 5 == 1)
                {
                    WriteMessage($"  Request timeout (attempt {attempt}), continuing...");
                }
            }

            // Linear backoff with max delay of 2 seconds
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            delay = Math.Min(delay + 250, 2000);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Waits for Gradio to be fully initialized by checking for Gradio-specific content in responses.
    /// This can take several minutes in CI while UV/Python dependencies are being installed.
    /// </summary>
    private async Task WaitForGradioReadyAsync(CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var attempt = 0;
        var startTime = DateTime.UtcNow;
        var lastStatusReport = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            var elapsed = DateTime.UtcNow - startTime;

            try
            {
                var response = await httpClient
                    .GetAsync(BaseUrl, HttpCompletionOption.ResponseContentRead, cancellationToken)
                    .ConfigureAwait(false);

                var statusCode = (int)response.StatusCode;

                // Log progress every 15 seconds to show the wait is active
                if ((DateTime.UtcNow - lastStatusReport).TotalSeconds >= 15)
                {
                    WriteMessage(
                        $"  Still waiting for Gradio (attempt {attempt}, elapsed: {elapsed.TotalSeconds:F0}s, last status: {statusCode})..."
                    );
                    lastStatusReport = DateTime.UtcNow;
                }

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
                        WriteMessage(
                            $"  Gradio UI detected after {attempt} attempts ({elapsed.TotalSeconds:F1}s)"
                        );
                        // Give it a brief moment to fully stabilize
                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // Got 200 but no Gradio content - log for diagnostics
                    WriteMessage(
                        $"  Got HTTP 200 but Gradio content not detected yet (attempt {attempt}, elapsed: {elapsed.TotalSeconds:F0}s)"
                    );
                }
                else if (
                    response.StatusCode == HttpStatusCode.BadGateway
                    || response.StatusCode == HttpStatusCode.ServiceUnavailable
                )
                {
                    // 502/503 means YARP is proxying but Gradio backend isn't ready yet
                    // This is expected during Gradio startup - don't spam logs
                }
                else
                {
                    // Unexpected status code - log it
                    WriteMessage(
                        $"  Unexpected status {statusCode} while waiting for Gradio (attempt {attempt})"
                    );
                }
            }
            catch (HttpRequestException)
            {
                // Connection issues, continue waiting
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout on individual request, continue polling
                WriteMessage(
                    $"  Request timeout (attempt {attempt}), Gradio may still be starting..."
                );
            }

            await Task.Delay(GradioPollingDelayMs, cancellationToken).ConfigureAwait(false);
        }

        // If we get here due to cancellation, throw to indicate failure
        cancellationToken.ThrowIfCancellationRequested();
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
