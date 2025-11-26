using System.Diagnostics;
using System.Net;
// ReSharper disable once RedundantUsingDirective - Required for type resolution
using System.Net.Http;
using System.Net.Sockets;

namespace RoslynStone.GradioTests;

/// <summary>
/// Shared fixture that starts the MCP server in HTTP mode with Gradio landing page.
/// Ensures Python dependencies are installed via UV at runtime for test isolation.
/// </summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    private Process? _serverProcess;

    /// <summary>
    /// Base URL of the running MCP server (e.g., http://localhost:5000)
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// MCP endpoint URL (BaseUrl + /mcp)
    /// </summary>
    public string McpEndpoint => $"{BaseUrl}/mcp";

    /// <summary>
    /// Root URL (Gradio landing page)
    /// </summary>
    public string GradioUrl => BaseUrl;

    async Task IAsyncLifetime.InitializeAsync()
    {
        // Find a free port for the test server
        var port = FindFreePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        // Determine if we're in CI or using pre-built assemblies
        var configuration = Environment.GetEnvironmentVariable("Configuration") ?? "Debug";
        var skipPythonInstall = Environment.GetEnvironmentVariable("SKIP_PYTHON_INSTALL") == "true";

        // Path to the built API assembly and its directory (contains pyproject.toml and will contain .venv)
        var apiOutputDir = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                $"../../../../../src/RoslynStone.Api/bin/{configuration}/net10.0"
            )
        );
        var apiAssemblyPath = Path.Combine(apiOutputDir, "RoslynStone.Api.dll");
        var venvPath = Path.Combine(apiOutputDir, ".venv");
        var pyprojectPath = Path.Combine(apiOutputDir, "pyproject.toml");

        // If not skipping Python install, ensure venv exists and install dependencies with UV
        if (!skipPythonInstall)
        {
            Console.WriteLine($"[FIXTURE] Setting up Python environment in {apiOutputDir}");

            // Create venv if it doesn't exist using UV (faster and compatible with UV pip)
            if (!Directory.Exists(venvPath))
            {
                Console.WriteLine($"[FIXTURE] Creating virtual environment at {venvPath} with UV");
                var createVenvProcess = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "uv",
                        Arguments = $"venv \"{venvPath}\" --python python3.12",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = apiOutputDir,
                    }
                );
                await createVenvProcess!.WaitForExitAsync();

                if (createVenvProcess.ExitCode != 0)
                {
                    var error = await createVenvProcess.StandardError.ReadToEndAsync();
                    Console.WriteLine($"[FIXTURE] Warning: Failed to create venv with UV: {error}");
                    Console.WriteLine(
                        "[FIXTURE] Continuing - CSnakes will handle Python environment"
                    );
                }
            }

            // Install dependencies with UV
            if (File.Exists(pyprojectPath) && Directory.Exists(venvPath))
            {
                Console.WriteLine(
                    $"[FIXTURE] Installing Python dependencies with UV from {pyprojectPath}"
                );
                var uvProcess = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "uv",
                        Arguments =
                            $"pip install --python \"{venvPath}/bin/python\" --prerelease=allow gradio>=6.0.0 httpx>=0.27.0 pygments>=2.17.0 openai>=1.0.0 anthropic>=0.25.0 google-generativeai>=0.3.0 huggingface_hub>=0.20.0",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = apiOutputDir,
                    }
                );

                if (uvProcess != null)
                {
                    uvProcess.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Console.WriteLine($"[UV] {e.Data}");
                    };
                    uvProcess.ErrorDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            Console.WriteLine($"[UV] {e.Data}");
                    };
                    uvProcess.BeginOutputReadLine();
                    uvProcess.BeginErrorReadLine();

                    await uvProcess.WaitForExitAsync();

                    if (uvProcess.ExitCode != 0)
                    {
                        Console.WriteLine(
                            "[FIXTURE] UV installation had issues, but continuing (CSnakes may install at runtime)"
                        );
                    }
                    else
                    {
                        Console.WriteLine("[FIXTURE] Python dependencies installed successfully");
                    }
                }
            }
        }

        // Configure environment for HTTP transport
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec \"{apiAssemblyPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = apiOutputDir,
        };

        // Set environment variables for HTTP mode
        startInfo.Environment["MCP_TRANSPORT"] = "http";
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        // Let CSnakes handle UV installation if we didn't pre-install
        startInfo.Environment["SKIP_PYTHON_INSTALL"] = "false";
        startInfo.Environment["UV_PRERELEASE"] = "allow";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        _serverProcess = Process.Start(startInfo);

        if (_serverProcess == null)
        {
            throw new InvalidOperationException("Failed to start MCP server process");
        }

        // Capture output for debugging
        _serverProcess.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine($"[SERVER] {e.Data}");
            }
        };
        _serverProcess.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.Error.WriteLine($"[SERVER ERROR] {e.Data}");
            }
        };
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        // Give server time to start and Gradio to initialize
        // Allow extra time for CSnakes/UV installation if needed
        var startupDelay = TimeSpan.FromSeconds(20);
        await Task.Delay(startupDelay);

        // Verify server is responding
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var maxRetries = 10;
        var retry = 0;
        while (retry < maxRetries)
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"[FIXTURE] Server is responding at {BaseUrl}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FIXTURE] Waiting for server (attempt {retry + 1}/{maxRetries}): {ex.Message}"
                );
            }

            retry++;
            await Task.Delay(1000);
        }

        if (retry >= maxRetries)
        {
            throw new TimeoutException(
                $"Server at {BaseUrl} did not respond after {maxRetries} attempts"
            );
        }
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            try
            {
                _serverProcess.Kill(true);
                _serverProcess.WaitForExit(5000);
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                _serverProcess.Dispose();
            }
        }

        return Task.CompletedTask;
    }

    private static int FindFreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }
}
