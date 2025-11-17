using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace RoslynStone.LoadTests;

public class Program
{
    private const int DefaultConcurrency = 300;
    private const int DefaultRounds = 10;
    private const string DefaultBaseUrl = "http://localhost:7071";

    public static async Task Main(string[] args)
    {
        // Parse command line arguments
        var baseUrl = args.Length > 0 ? args[0] : DefaultBaseUrl;
        var concurrency = args.Length > 1 ? int.Parse(args[1]) : DefaultConcurrency;
        var rounds = args.Length > 2 ? int.Parse(args[2]) : DefaultRounds;

        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║       Roslyn-Stone MCP Server Load Test              ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Base URL:     {baseUrl}");
        Console.WriteLine($"  Concurrency:  {concurrency}");
        Console.WriteLine($"  Rounds:       {rounds}");
        Console.WriteLine();

        // Check if server is available
        Console.WriteLine("Checking server availability...");
        using var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30),
        };

        try
        {
            var healthCheck = await client.GetAsync("/health");
            if (!healthCheck.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️  Server health check returned {healthCheck.StatusCode}");
            }
            else
            {
                Console.WriteLine("✅ Server is available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Server is not available: {ex.Message}");
            Console.WriteLine("\nPlease start the server with:");
            Console.WriteLine("  cd src/RoslynStone.Api");
            Console.WriteLine("  MCP_TRANSPORT=http dotnet run");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Starting load test...");
        Console.WriteLine();

        var loadTester = new LoadTester(client, concurrency, rounds);
        var results = await loadTester.RunAsync();

        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   Test Results                        ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════╝");
        Console.WriteLine();

        results.PrintSummary();
    }
}

public class LoadTester
{
    private readonly HttpClient _client;
    private readonly int _concurrency;
    private readonly int _rounds;

    public LoadTester(HttpClient client, int concurrency, int rounds)
    {
        _client = client;
        _concurrency = concurrency;
        _rounds = rounds;
    }

    public async Task<LoadTestResults> RunAsync()
    {
        var results = new LoadTestResults(_concurrency, _rounds);

        // Test scenarios
        var scenarios = new[]
        {
            ("Simple Expression", CreateSimpleExpressionRequest()),
            ("Variable Assignment", CreateVariableAssignmentRequest()),
            ("LINQ Query", CreateLinqQueryRequest()),
            ("NuGet Search", CreateNuGetSearchRequest()),
        };

        foreach (var (scenarioName, requestContent) in scenarios)
        {
            Console.WriteLine($"Testing scenario: {scenarioName}");
            var scenarioResults = await RunScenarioAsync(requestContent);
            results.AddScenario(scenarioName, scenarioResults);
            Console.WriteLine($"  ✅ Completed");
        }

        return results;
    }

    private async Task<ScenarioResults> RunScenarioAsync(string requestContent)
    {
        var scenarioResults = new ScenarioResults(_rounds);

        for (int round = 0; round < _rounds; round++)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<RoundResult>>();

            for (int i = 0; i < _concurrency; i++)
            {
                tasks.Add(ExecuteRequestAsync(requestContent));
            }

            var roundResults = await Task.WhenAll(tasks);
            sw.Stop();

            var successCount = roundResults.Count(r => r.Success);
            var failureCount = roundResults.Count(r => !r.Success);
            var avgResponseTime = roundResults.Where(r => r.Success).Average(r => r.ResponseTimeMs);

            scenarioResults.AddRound(
                round,
                sw.ElapsedMilliseconds,
                successCount,
                failureCount,
                avgResponseTime
            );
        }

        return scenarioResults;
    }

    private async Task<RoundResult> ExecuteRequestAsync(string requestContent)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var content = new StringContent(requestContent, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/mcp", content);
            sw.Stop();

            return new RoundResult
            {
                Success = response.IsSuccessStatusCode,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                StatusCode = (int)response.StatusCode,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RoundResult
            {
                Success = false,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
            };
        }
    }

    private static string CreateSimpleExpressionRequest()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new { name = "EvaluateCsharp", arguments = new { code = "2 + 2" } },
            id = 1,
        };
        return JsonSerializer.Serialize(request);
    }

    private static string CreateVariableAssignmentRequest()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "EvaluateCsharp",
                arguments = new { code = "var x = 10; x * 2" },
            },
            id = 1,
        };
        return JsonSerializer.Serialize(request);
    }

    private static string CreateLinqQueryRequest()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "EvaluateCsharp",
                arguments = new { code = "Enumerable.Range(1, 100).Where(x => x % 2 == 0).Sum()" },
            },
            id = 1,
        };
        return JsonSerializer.Serialize(request);
    }

    private static string CreateNuGetSearchRequest()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "SearchNuGetPackages",
                arguments = new
                {
                    query = "Newtonsoft.Json",
                    skip = 0,
                    take = 10,
                },
            },
            id = 1,
        };
        return JsonSerializer.Serialize(request);
    }
}

public record RoundResult
{
    public bool Success { get; init; }
    public long ResponseTimeMs { get; init; }
    public int StatusCode { get; init; }
    public string? Error { get; init; }
}

public class ScenarioResults
{
    private readonly List<(
        int Round,
        long TotalMs,
        int Success,
        int Failures,
        double AvgResponseMs
    )> _rounds = new();
    private readonly int _expectedRounds;

    public ScenarioResults(int expectedRounds)
    {
        _expectedRounds = expectedRounds;
    }

    public void AddRound(
        int round,
        long totalMs,
        int successCount,
        int failureCount,
        double avgResponseMs
    )
    {
        _rounds.Add((round, totalMs, successCount, failureCount, avgResponseMs));
    }

    public double AverageTotalTimeMs => _rounds.Average(r => r.TotalMs);
    public double AverageResponseTimeMs => _rounds.Average(r => r.AvgResponseMs);
    public int TotalSuccess => _rounds.Sum(r => r.Success);
    public int TotalFailures => _rounds.Sum(r => r.Failures);
    public double SuccessRate => TotalSuccess / (double)(TotalSuccess + TotalFailures) * 100;

    public IReadOnlyList<(
        int Round,
        long TotalMs,
        int Success,
        int Failures,
        double AvgResponseMs
    )> Rounds => _rounds;
}

public class LoadTestResults
{
    private readonly Dictionary<string, ScenarioResults> _scenarios = new();
    private readonly int _concurrency;
    private readonly int _rounds;

    public LoadTestResults(int concurrency, int rounds)
    {
        _concurrency = concurrency;
        _rounds = rounds;
    }

    public void AddScenario(string name, ScenarioResults results)
    {
        _scenarios[name] = results;
    }

    public void PrintSummary()
    {
        Console.WriteLine($"Overall Statistics:");
        Console.WriteLine($"  Concurrency: {_concurrency} requests/round");
        Console.WriteLine($"  Rounds: {_rounds}");
        Console.WriteLine($"  Total Requests: {_concurrency * _rounds * _scenarios.Count}");
        Console.WriteLine();

        foreach (var (scenarioName, results) in _scenarios)
        {
            Console.WriteLine($"Scenario: {scenarioName}");
            Console.WriteLine($"  Average Round Time:     {results.AverageTotalTimeMs:F2} ms");
            Console.WriteLine($"  Average Response Time:  {results.AverageResponseTimeMs:F2} ms");
            Console.WriteLine($"  Success Rate:           {results.SuccessRate:F2}%");
            Console.WriteLine(
                $"  Total Success:          {results.TotalSuccess}/{results.TotalSuccess + results.TotalFailures}"
            );

            if (results.TotalFailures > 0)
            {
                Console.WriteLine($"  ⚠️  Total Failures:        {results.TotalFailures}");
            }

            // Calculate requests per second
            var rps = _concurrency / (results.AverageTotalTimeMs / 1000.0);
            Console.WriteLine($"  Throughput:             {rps:F2} requests/second");
            Console.WriteLine();
        }

        // Overall success rate
        var totalSuccess = _scenarios.Values.Sum(r => r.TotalSuccess);
        var totalRequests = _scenarios.Values.Sum(r => r.TotalSuccess + r.TotalFailures);
        var overallSuccessRate = totalSuccess / (double)totalRequests * 100;

        Console.WriteLine($"Overall Success Rate: {overallSuccessRate:F2}%");

        if (overallSuccessRate < 99.0)
        {
            Console.WriteLine(
                "⚠️  Warning: Success rate is below 99%. Check server logs for errors."
            );
        }
        else
        {
            Console.WriteLine("✅ All tests passed with high success rate!");
        }
    }
}
